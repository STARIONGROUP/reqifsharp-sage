// -------------------------------------------------------------------------------------------------
// <copyright file="CliProvisioner.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Hook
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Downloads, SHA-256-verifies, and caches the full <c>reqifsharp-sage</c> CLI from a pinned GitHub
    /// Release the first time it's actually needed - the SessionStart hook itself stays a small,
    /// dependency-free binary (see the root CLAUDE.md's "small hook, big CLI" split); the CLI's heavier
    /// dependencies (<c>ReqIFSharp</c>, Spectre.Console, System.CommandLine) are not AOT-trim-friendly,
    /// which is why they live in a separately-published, on-demand-downloaded executable instead.
    /// </summary>
    public sealed class CliProvisioner
    {
        /// <summary>
        /// The GitHub repository the full CLI is released from.
        /// </summary>
        public const string Repository = "STARIONGROUP/reqifsharp-sage";

        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="CliProvisioner"/> class.
        /// </summary>
        public CliProvisioner(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        /// <summary>
        /// Ensures the CLI for <paramref name="cliVersion"/> is present and verified in the local cache,
        /// downloading and extracting it if necessary. Never throws: any failure (offline, rate-limited,
        /// no matching asset, checksum mismatch) results in a <c>null</c> return, meaning the hook simply
        /// has nothing to report this session and will try again next time.
        /// </summary>
        /// <returns>
        /// The path to the verified, ready-to-run CLI executable, or <c>null</c> if it could not be provisioned.
        /// </returns>
        public async Task<string?> EnsureAsync(string pluginRoot, string cliVersion, CancellationToken cancellationToken)
        {
            try
            {
                var runtimeIdentifier = RuntimeIdentifier.Current();
                if (runtimeIdentifier is null)
                {
                    return null;
                }

                var cliDirectory = CacheLayout.CliDirectoryFor(pluginRoot, cliVersion, runtimeIdentifier);
                var executablePath = Path.Combine(cliDirectory, RuntimeIdentifier.ExecutableName(runtimeIdentifier));

                if (File.Exists(CacheLayout.VerifiedMarkerPath(cliDirectory)) && File.Exists(executablePath))
                {
                    return executablePath;
                }

                return await this.DownloadAndExtractAsync(cliVersion, runtimeIdentifier, cliDirectory, executablePath, cancellationToken);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Returns the cached, verified CLI for <paramref name="cliVersion"/> without any network access,
        /// or <c>null</c> when it has not been provisioned yet.
        /// </summary>
        /// <param name="pluginRoot">The plugin root.</param>
        /// <param name="cliVersion">The pinned CLI version.</param>
        /// <returns>The executable path, or null.</returns>
        public static string? TryGetCached(string pluginRoot, string cliVersion)
        {
            var runtimeIdentifier = RuntimeIdentifier.Current();
            if (runtimeIdentifier is null)
            {
                return null;
            }

            var cliDirectory = CacheLayout.CliDirectoryFor(pluginRoot, cliVersion, runtimeIdentifier);
            var executablePath = Path.Combine(cliDirectory, RuntimeIdentifier.ExecutableName(runtimeIdentifier));
            return File.Exists(CacheLayout.VerifiedMarkerPath(cliDirectory)) && File.Exists(executablePath) ? executablePath : null;
        }

        private async Task<string?> DownloadAndExtractAsync(string cliVersion, string runtimeIdentifier, string cliDirectory, string executablePath, CancellationToken cancellationToken)
        {
            var release = await this.FetchReleaseAsync(cliVersion, cancellationToken);
            if (release is null)
            {
                return null;
            }

            var assetName = $"reqifsharp-sage-{cliVersion}-{runtimeIdentifier}.zip";
            var asset = release.Assets.Find(a => a.Name == assetName);
            if (asset is null)
            {
                return null;
            }

            var checksums = ChecksumTable.Parse(release.Body);
            if (!checksums.TryGetValue(assetName, out var expectedSha256))
            {
                return null;
            }

            var zipBytes = await this.httpClient.GetByteArrayAsync(asset.BrowserDownloadUrl, cancellationToken);
            var actualSha256 = Convert.ToHexStringLower(SHA256.HashData(zipBytes));
            if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            Directory.CreateDirectory(cliDirectory);
            var zipPath = Path.Combine(Path.GetTempPath(), $"{assetName}-{Guid.NewGuid():N}.zip");
            try
            {
                await File.WriteAllBytesAsync(zipPath, zipBytes, cancellationToken);
                ZipFile.ExtractToDirectory(zipPath, cliDirectory, overwriteFiles: true);
            }
            finally
            {
                File.Delete(zipPath);
            }

            if (!File.Exists(executablePath))
            {
                return null;
            }

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(executablePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                    | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            await File.WriteAllTextAsync(CacheLayout.VerifiedMarkerPath(cliDirectory), DateTimeOffset.UtcNow.ToString("o"), cancellationToken);
            return executablePath;
        }

        private async Task<GitHubRelease?> FetchReleaseAsync(string cliVersion, CancellationToken cancellationToken)
        {
            var url = $"https://api.github.com/repos/{Repository}/releases/tags/tools-v{cliVersion}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("reqifsharp-sage-hook");

            using var response = await this.httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync(stream, HookJsonContext.Default.GitHubRelease, cancellationToken);
        }
    }
}
