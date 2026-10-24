// -------------------------------------------------------------------------------------------------
// <copyright file="GuideImporter.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    using System;
    using System.IO;
    using System.Text.Json;

    /// <summary>
    /// Imports a user-supplied copy of the prostep ivip ReqIF Implementation Guide into the git-ignored
    /// <c>sources/psi-guide/&lt;version&gt;/</c>. The guide is not public: this tool never downloads it,
    /// and nothing derived from it is committed.
    /// </summary>
    public static class GuideImporter
    {
        private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        /// <summary>
        /// Copies <paramref name="pdfPath"/> into the layout.
        /// </summary>
        /// <param name="layout">The repository layout.</param>
        /// <param name="pdfPath">The user's copy of the guide PDF.</param>
        /// <param name="version">
        /// The guide version. Optional when the file is a known release (recognised by its SHA-256);
        /// required otherwise.
        /// </param>
        /// <returns>The import result.</returns>
        /// <exception cref="FileNotFoundException">The PDF does not exist.</exception>
        /// <exception cref="InvalidDataException">The file is not a PDF.</exception>
        /// <exception cref="ArgumentException">The file is not a known release and no version was given.</exception>
        public static GuideImportResult Import(RepositoryLayout layout, string pdfPath, string? version)
        {
            if (!File.Exists(pdfPath))
            {
                throw new FileNotFoundException($"The implementation guide PDF {pdfPath} does not exist.", pdfPath);
            }

            var bytes = File.ReadAllBytes(pdfPath);
            if (bytes.Length < 4 || bytes[0] != (byte)'%' || bytes[1] != (byte)'P' || bytes[2] != (byte)'D' || bytes[3] != (byte)'F')
            {
                throw new InvalidDataException($"{pdfPath} is not a PDF file.");
            }

            var sha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
            var known = KnownGuideVersions.FindBySha256(sha256);

            string? warning = null;
            if (version is null)
            {
                if (known is null)
                {
                    throw new ArgumentException("The PDF is not a known release of the implementation guide; pass --version to say which version it is.", nameof(version));
                }

                version = known.Version;
            }
            else if (known is null)
            {
                warning = $"The PDF is not a known release of the implementation guide; it is recorded as version {version} as requested.";
            }
            else if (known.Version != version)
            {
                warning = $"The PDF is the known release {known.Version}, but it is recorded as version {version} as requested.";
            }

            var descriptor = KnownGuideVersions.Find(version);
            var fileName = descriptor?.FileName ?? $"PSI-ReqIF-Implementation-Guide-{version}.pdf";
            var directory = layout.GuideSourcesDirectoryFor(version);
            Directory.CreateDirectory(directory);

            var destination = Path.Combine(directory, fileName);
            File.WriteAllBytes(destination, bytes);

            var manifest = new GuideImportManifest(version, fileName, sha256, known is not null, DateTimeOffset.UtcNow);
            File.WriteAllText(Path.Combine(directory, "import-manifest.json"), JsonSerializer.Serialize(manifest, SerializerOptions));

            new InstalledVersionsStore(layout.KnowledgeRoot).MarkGuideImported(version);

            return new GuideImportResult(version, destination, known is not null, warning);
        }

        /// <summary>
        /// Gets the path of an imported guide PDF, or null when that version has not been imported.
        /// </summary>
        /// <param name="layout">The repository layout.</param>
        /// <param name="version">The guide version.</param>
        /// <returns>The PDF path, or null.</returns>
        public static string? ImportedPdfPathFor(RepositoryLayout layout, string version)
        {
            var manifestPath = Path.Combine(layout.GuideSourcesDirectoryFor(version), "import-manifest.json");
            if (!File.Exists(manifestPath))
            {
                return null;
            }

            var manifest = JsonSerializer.Deserialize<GuideImportManifest>(File.ReadAllText(manifestPath), SerializerOptions);
            var path = manifest is null ? null : Path.Combine(layout.GuideSourcesDirectoryFor(version), manifest.File);
            return path is not null && File.Exists(path) ? path : null;
        }
    }

    /// <summary>
    /// The outcome of <see cref="GuideImporter.Import"/>.
    /// </summary>
    /// <param name="Version">The version the guide was recorded as.</param>
    /// <param name="StoredPath">Where the PDF was stored.</param>
    /// <param name="IsKnownRelease">Whether the file is a known release.</param>
    /// <param name="Warning">A warning to show the user, if any.</param>
    public sealed record GuideImportResult(string Version, string StoredPath, bool IsKnownRelease, string? Warning);

    /// <summary>
    /// The contents of <c>sources/psi-guide/&lt;version&gt;/import-manifest.json</c>.
    /// </summary>
    /// <param name="Version">The guide version.</param>
    /// <param name="File">The stored file name.</param>
    /// <param name="Sha256">The SHA-256 of the stored file.</param>
    /// <param name="IsKnownRelease">Whether the file is a known release.</param>
    /// <param name="ImportedAt">When it was imported.</param>
    public sealed record GuideImportManifest(string Version, string File, string Sha256, bool IsKnownRelease, DateTimeOffset ImportedAt);
}
