// -------------------------------------------------------------------------------------------------
// <copyright file="UvProvisioner.cs" company="Starion Group S.A.">
//
//   Copyright (C) 2026 Starion Group S.A.
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge.Toolchain
{
    using System;
    using System.Formats.Tar;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Fetches, checksum-verifies and caches the <c>uv</c> binary from its GitHub releases the first
    /// time it is needed.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>ReqIFSharp.Sage.Tools.Hook</c>'s <c>CliProvisioner</c> architecturally (cache-check,
    /// download, checksum-verify, extract, mark-verified, never throw) but is independent code: the
    /// hook project is deliberately kept dependency-free and NativeAOT-publishable, so neither project
    /// references the other.
    /// <para>
    /// Never throws past <see cref="EnsureAsync"/>: any failure here (offline, rate-limited, a corrupt
    /// download, an unsupported platform) means the caller degrades to a
    /// <see cref="SpecExtractionOutcome.Skipped"/> rather than aborting the rest of a
    /// <c>generate</c> run.
    /// </para>
    /// </remarks>
    public sealed class UvProvisioner : IUvProvisioner
    {
        private const string Repository = "astral-sh/uv";

        /// <summary>The pinned uv release. Bumped by PR, the same way the CLI's own release is.</summary>
        /// <remarks>Public so tests can address the cache path this provisions without duplicating the value.</remarks>
        public const string PinnedVersion = "0.12.10";

        private const string CacheFolderName = "reqifsharp-sage";

        private readonly HttpClient client;
        private readonly DirectoryInfo cacheRoot;

        /// <summary>
        /// Initializes a new instance of the <see cref="UvProvisioner"/> class.
        /// </summary>
        public UvProvisioner(HttpClient client)
            : this(client, ResolveCacheRoot())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UvProvisioner"/> class with an explicit cache
        /// root, so a test never touches the real <c>%LOCALAPPDATA%</c>.
        /// </summary>
        public UvProvisioner(HttpClient client, DirectoryInfo cacheRoot)
        {
            ArgumentNullException.ThrowIfNull(client);
            ArgumentNullException.ThrowIfNull(cacheRoot);

            this.client = client;
            this.cacheRoot = cacheRoot;
        }

        /// <summary>
        /// Resolves the cache root via <see cref="Environment.SpecialFolder.LocalApplicationData"/> -
        /// the same folder the CLI's own self-fetch hook (<c>CacheLayout.CacheRoot</c>) already caches
        /// into, so this is a sibling, not a new location to document.
        /// </summary>
        public static DirectoryInfo ResolveCacheRoot(Func<Environment.SpecialFolder, string>? getFolderPath = null)
        {
            getFolderPath ??= Environment.GetFolderPath;

            return new DirectoryInfo(
                Path.Combine(getFolderPath(Environment.SpecialFolder.LocalApplicationData), CacheFolderName));
        }

        /// <summary><c>uv/&lt;version&gt;/&lt;target&gt;/</c> - where one uv build is cached.</summary>
        public DirectoryInfo BinDirectory(string version, string target) =>
            new(Path.Combine(this.cacheRoot.FullName, "uv", version, target));

        /// <summary>The cached <c>uv</c> executable itself.</summary>
        public FileInfo Executable(string version, string target) =>
            new(Path.Combine(this.BinDirectory(version, target).FullName, UvPlatform.ExecutableName(target)));

        /// <summary>
        /// Written only once the cached download's checksum has been verified - its presence is what
        /// lets a later run skip the network entirely.
        /// </summary>
        public FileInfo OkMarker(string version, string target) =>
            new(Path.Combine(this.BinDirectory(version, target).FullName, ".ok"));

        /// <inheritdoc/>
        public async Task<FileInfo?> EnsureAsync(CancellationToken cancellationToken = default)
        {
            var target = UvPlatform.CurrentTarget;
            if (target is null)
            {
                return null;
            }

            var executable = this.Executable(PinnedVersion, target);

            if (this.IsCached(PinnedVersion, target))
            {
                return executable;
            }

            try
            {
                var assetName = UvPlatform.AssetName(target);

                using var releaseRequest = new HttpRequestMessage(
                    HttpMethod.Get, $"https://api.github.com/repos/{Repository}/releases/tags/{PinnedVersion}");
                releaseRequest.Headers.UserAgent.ParseAdd("reqifsharp-sage");

                using var releaseResponse = await this.client.SendAsync(releaseRequest, cancellationToken);
                if (!releaseResponse.IsSuccessStatusCode)
                {
                    return null;
                }

                var releasePayload = await releaseResponse.Content.ReadAsStringAsync(cancellationToken);
                using var release = JsonDocument.Parse(releasePayload);

                var (downloadUrl, checksumUrl) = FindAssetUrls(release.RootElement, assetName);
                if (downloadUrl is null || checksumUrl is null)
                {
                    return null;
                }

                var expectedSha256 = await this.ReadChecksumAsync(checksumUrl, cancellationToken);
                if (expectedSha256 is null)
                {
                    return null;
                }

                var binDirectory = this.BinDirectory(PinnedVersion, target);
                binDirectory.Create();

                var archivePath = Path.Combine(binDirectory.FullName, assetName + ".download");

                await using (var archiveStream = File.Create(archivePath))
                await using (var downloadStream = await this.client.GetStreamAsync(downloadUrl, cancellationToken))
                {
                    await downloadStream.CopyToAsync(archiveStream, cancellationToken);
                }

                if (!VerifySha256(archivePath, expectedSha256))
                {
                    File.Delete(archivePath);

                    return null;
                }

                Extract(archivePath, binDirectory.FullName, target, executable.Name);
                File.Delete(archivePath);

                executable.Refresh();
                if (!executable.Exists)
                {
                    return null;
                }

                if (!OperatingSystem.IsWindows())
                {
                    File.SetUnixFileMode(
                        executable.FullName,
                        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                        | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                        | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
                }

                // Written last, only once the executable is in place: its presence is what IsCached trusts
                // to skip the network on every later run.
                await File.WriteAllTextAsync(
                    this.OkMarker(PinnedVersion, target).FullName, expectedSha256, cancellationToken);

                return executable;
            }
            catch (Exception exception) when (
                exception is HttpRequestException or IOException or JsonException
                or TaskCanceledException or InvalidOperationException)
            {
                return null;
            }
        }

        private bool IsCached(string version, string target)
        {
            var executable = this.Executable(version, target);
            var marker = this.OkMarker(version, target);

            executable.Refresh();
            marker.Refresh();

            return executable.Exists && marker.Exists;
        }

        /// <summary>uv publishes each asset's checksum as a separate, same-named ``.sha256`` asset.</summary>
        private static (string? DownloadUrl, string? ChecksumUrl) FindAssetUrls(
            JsonElement release, string assetName)
        {
            string? downloadUrl = null;
            string? checksumUrl = null;

            foreach (var asset in release.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();

                if (string.Equals(name, assetName, StringComparison.Ordinal))
                {
                    downloadUrl = asset.GetProperty("browser_download_url").GetString();
                }
                else if (string.Equals(name, assetName + ".sha256", StringComparison.Ordinal))
                {
                    checksumUrl = asset.GetProperty("browser_download_url").GetString();
                }
            }

            return (downloadUrl, checksumUrl);
        }

        /// <summary>Reads a `sha256sum`-format checksum file: "&lt;hex&gt; *&lt;filename&gt;".</summary>
        private async Task<string?> ReadChecksumAsync(string checksumUrl, CancellationToken cancellationToken)
        {
            var content = await this.client.GetStringAsync(checksumUrl, cancellationToken);
            var hex = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            return string.IsNullOrWhiteSpace(hex) ? null : hex;
        }

        private static bool VerifySha256(string path, string expectedHex)
        {
            using var stream = File.OpenRead(path);
            var actual = Convert.ToHexStringLower(SHA256.HashData(stream));

            return string.Equals(actual, expectedHex, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Extracts the archive and places its files directly in <paramref name="destination"/>. uv's
        /// archives are not all flat: the Linux and macOS tarballs wrap the binaries in a
        /// <c>uv-&lt;target&gt;/</c> folder, so the archive is unpacked into a staging folder first and the
        /// folder that holds <paramref name="executableName"/> is moved into place.
        /// </summary>
        private static void Extract(string archivePath, string destination, string target, string executableName)
        {
            var staging = Path.Combine(destination, ".extract");
            if (Directory.Exists(staging))
            {
                Directory.Delete(staging, recursive: true);
            }

            Directory.CreateDirectory(staging);

            try
            {
                if (UvPlatform.IsZip(target))
                {
                    ZipFile.ExtractToDirectory(archivePath, staging, overwriteFiles: true);
                }
                else
                {
                    using var fileStream = File.OpenRead(archivePath);
                    using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
                    TarFile.ExtractToDirectory(gzip, staging, overwriteFiles: true);
                }

                var found = Directory.EnumerateFiles(staging, executableName, SearchOption.AllDirectories)
                    .OrderBy(path => path.Length)
                    .FirstOrDefault();

                if (found is null)
                {
                    return;
                }

                foreach (var file in Directory.EnumerateFiles(Path.GetDirectoryName(found)!))
                {
                    File.Move(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
                }
            }
            finally
            {
                Directory.Delete(staging, recursive: true);
            }
        }
    }
}
