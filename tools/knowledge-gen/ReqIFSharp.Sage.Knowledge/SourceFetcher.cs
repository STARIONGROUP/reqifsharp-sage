// -------------------------------------------------------------------------------------------------
// <copyright file="SourceFetcher.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Polly;
    using Polly.Retry;

    /// <summary>
    /// Stores the OMG source files of a ReqIF version under <c>sources/&lt;version&gt;/</c>, either by
    /// downloading them from omg.org or by copying them from a local directory (<c>fetch --from</c>),
    /// and records their provenance in <c>fetch-manifest.json</c>.
    /// </summary>
    public sealed class SourceFetcher
    {
        private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private readonly HttpClient httpClient;

        private readonly AsyncRetryPolicy retryPolicy;

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceFetcher"/> class.
        /// </summary>
        /// <param name="httpClient">The client used for downloads (injectable so tests can stub it).</param>
        public SourceFetcher(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            this.retryPolicy = Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(250 * Math.Pow(2, attempt)));
        }

        /// <summary>
        /// Stores the source files of <paramref name="descriptor"/> under
        /// <paramref name="versionDirectory"/>.
        /// </summary>
        /// <param name="descriptor">The ReqIF version.</param>
        /// <param name="versionDirectory">The target, <c>sources/&lt;version&gt;</c>.</param>
        /// <param name="includeSpecs">Whether to include the specification PDF.</param>
        /// <param name="fromDirectory">
        /// A local directory to take the files from instead of downloading them. A file is recognised by
        /// its SHA-256 first (so <c>dtc-10-12-13.cmof.xml</c> is found as <c>reqif.cmof</c>), then by name.
        /// Files not found there are downloaded.
        /// </param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The manifest entries, one per stored file.</returns>
        public async Task<IReadOnlyList<FetchManifestEntry>> FetchAsync(ReqIFVersionDescriptor descriptor, string versionDirectory, bool includeSpecs, string? fromDirectory, CancellationToken cancellationToken = default)
        {
            var localCandidates = fromDirectory is null ? [] : IndexLocalFiles(fromDirectory);
            var entries = new List<FetchManifestEntry>();

            foreach (var file in descriptor.Files.Where(f => includeSpecs || !f.IsSpecificationDocument))
            {
                var destination = Path.Combine(versionDirectory, file.RelativePath.Replace('/', Path.DirectorySeparatorChar));
                var local = FindLocal(localCandidates, file);

                entries.Add(local is not null
                    ? CopyLocal(file, local.Value.Path, local.Value.Sha256, destination)
                    : await this.DownloadAsync(file, destination, cancellationToken));
            }

            Directory.CreateDirectory(versionDirectory);
            await File.WriteAllTextAsync(Path.Combine(versionDirectory, "fetch-manifest.json"), JsonSerializer.Serialize(entries, SerializerOptions), cancellationToken);

            return entries;
        }

        /// <summary>
        /// Computes the lower-case hex SHA-256 of a file.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The hash.</returns>
        public static string Sha256Of(string path)
        {
            using var stream = File.OpenRead(path);
            return Convert.ToHexStringLower(SHA256.HashData(stream));
        }

        private static List<(string Path, string Sha256)> IndexLocalFiles(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException($"The --from directory {directory} does not exist.");
            }

            return Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly)
                .Where(p => new FileInfo(p).Length < 64L * 1024 * 1024)
                .Select(p => (Path: p, Sha256: Sha256Of(p)))
                .OrderBy(p => p.Path, StringComparer.Ordinal)
                .ToList();
        }

        private static (string Path, string Sha256)? FindLocal(IReadOnlyList<(string Path, string Sha256)> candidates, SourceFile file)
        {
            foreach (var candidate in candidates)
            {
                if (candidate.Sha256 == file.Sha256)
                {
                    return candidate;
                }
            }

            foreach (var candidate in candidates)
            {
                if (string.Equals(Path.GetFileName(candidate.Path), file.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static FetchManifestEntry CopyLocal(SourceFile file, string sourcePath, string sha256, string destination)
        {
            var bytes = File.ReadAllBytes(sourcePath);
            Validate(file, bytes, sourcePath);

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.WriteAllBytes(destination, bytes);

            return new FetchManifestEntry(file.Url, file.RelativePath, sha256, sha256 == file.Sha256, "local", DateTimeOffset.UtcNow);
        }

        private async Task<FetchManifestEntry> DownloadAsync(SourceFile file, string destination, CancellationToken cancellationToken)
        {
            var bytes = await this.retryPolicy.ExecuteAsync(async () =>
            {
                using var response = await this.httpClient.GetAsync(file.Url, cancellationToken);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync(cancellationToken);
            });

            Validate(file, bytes, file.Url);

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            await File.WriteAllBytesAsync(destination, bytes, cancellationToken);

            var sha256 = Convert.ToHexStringLower(SHA256.HashData(bytes));
            return new FetchManifestEntry(file.Url, file.RelativePath, sha256, sha256 == file.Sha256, "download", DateTimeOffset.UtcNow);
        }

        private static void Validate(SourceFile file, byte[] bytes, string origin)
        {
            if (file.Role == SourceFileRole.SpecificationPdf && !HasPdfMagicBytes(bytes))
            {
                throw new InvalidDataException($"{origin} is not a PDF file (it does not start with \"%PDF\"); the server may have returned an error page.");
            }

            if (file.Role != SourceFileRole.SpecificationPdf && !LooksLikeXml(bytes))
            {
                throw new InvalidDataException($"{origin} is not an XML file (the first non-blank character is not \"<\"); the server may have returned an error page.");
            }
        }

        private static bool HasPdfMagicBytes(byte[] bytes)
        {
            ReadOnlySpan<byte> pdfMagic = "%PDF"u8;
            return bytes.Length >= pdfMagic.Length && bytes.AsSpan(0, pdfMagic.Length).SequenceEqual(pdfMagic);
        }

        private static bool LooksLikeXml(byte[] bytes)
        {
            var span = bytes.AsSpan();
            if (span.Length >= 3 && span[0] == 0xEF && span[1] == 0xBB && span[2] == 0xBF)
            {
                span = span[3..];
            }

            var index = 0;
            while (index < span.Length && char.IsWhiteSpace((char)span[index]))
            {
                index++;
            }

            return index < span.Length && span[index] == (byte)'<';
        }
    }
}
