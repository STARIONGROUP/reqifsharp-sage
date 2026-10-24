// -------------------------------------------------------------------------------------------------
// <copyright file="UvProvisionerTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge.Tests
{
    using System;
    using System.Formats.Tar;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    using ReqIFSharp.Sage.Knowledge.Toolchain;

    [TestFixture]
    public class UvProvisionerTests
    {
        private DirectoryInfo cacheRoot = null!;
        private string target = null!;

        [SetUp]
        public void SetUp()
        {
            this.cacheRoot = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "reqifsharp-sage-knowledge-tests", Guid.NewGuid().ToString("N")));
            this.target = UvPlatform.CurrentTarget ?? throw new InvalidOperationException("This test machine's platform is not one of uv's published targets - cannot run.");
        }

        [TearDown]
        public void TearDown()
        {
            if (this.cacheRoot.Exists)
            {
                this.cacheRoot.Delete(recursive: true);
            }
        }

        /// <summary>
        /// Builds an archive in the format uv publishes for <paramref name="target"/> (zip on Windows,
        /// tar.gz elsewhere), optionally wrapping the entry in a folder the way the real Linux/macOS
        /// tarballs do (<c>uv-&lt;target&gt;/uv</c>).
        /// </summary>
        private static byte[] BuildArchiveContaining(string target, string entryName, byte[] entryContent, string? folder = null)
        {
            var path = folder is null ? entryName : $"{folder}/{entryName}";
            using var memoryStream = new MemoryStream();

            if (UvPlatform.IsZip(target))
            {
                using var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true);
                var entry = archive.CreateEntry(path);
                using var entryStream = entry.Open();
                entryStream.Write(entryContent);
            }
            else
            {
                using var gzip = new GZipStream(memoryStream, CompressionLevel.Fastest, leaveOpen: true);
                using var writer = new TarWriter(gzip, leaveOpen: true);
                writer.WriteEntry(new PaxTarEntry(TarEntryType.RegularFile, path) { DataStream = new MemoryStream(entryContent) });
            }

            return memoryStream.ToArray();
        }

        private static StubHttpMessageHandler ReleaseServing(string assetName, byte[] archiveBytes)
        {
            var releaseJson = JsonSerializer.Serialize(new
            {
                assets = new object[]
                {
                    new { name = assetName, browser_download_url = "https://example.com/download/" + assetName },
                    new { name = assetName + ".sha256", browser_download_url = "https://example.com/download/" + assetName + ".sha256" },
                },
            });

            var handler = new StubHttpMessageHandler();
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releaseJson) });
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{Convert.ToHexStringLower(SHA256.HashData(archiveBytes))} *{assetName}") });
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(archiveBytes) });
            return handler;
        }

        [Test]
        public async Task EnsureAsync_finds_the_executable_inside_a_wrapping_folder_like_the_real_unix_tarballs()
        {
            var executableName = UvPlatform.ExecutableName(this.target);
            var archiveBytes = BuildArchiveContaining(this.target, executableName, Encoding.UTF8.GetBytes("fake uv"), folder: $"uv-{this.target}");
            var provisioner = new UvProvisioner(new HttpClient(ReleaseServing(UvPlatform.AssetName(this.target), archiveBytes)), this.cacheRoot);

            var executable = await provisioner.EnsureAsync();

            Assert.That(executable, Is.Not.Null);
            Assert.That(executable!.Name, Is.EqualTo(executableName));
            Assert.That(File.ReadAllText(executable.FullName), Is.EqualTo("fake uv"));
            Assert.That(Directory.Exists(Path.Combine(executable.DirectoryName!, ".extract")), Is.False);
            Assert.That(provisioner.OkMarker(UvProvisioner.PinnedVersion, this.target).Exists, Is.True);
        }

        [Test]
        public async Task EnsureAsync_returns_null_and_writes_no_marker_when_the_archive_lacks_the_executable()
        {
            var archiveBytes = BuildArchiveContaining(this.target, "README.md", Encoding.UTF8.GetBytes("no uv here"));
            var provisioner = new UvProvisioner(new HttpClient(ReleaseServing(UvPlatform.AssetName(this.target), archiveBytes)), this.cacheRoot);

            var executable = await provisioner.EnsureAsync();

            Assert.That(executable, Is.Null);
            Assert.That(provisioner.OkMarker(UvProvisioner.PinnedVersion, this.target).Exists, Is.False);
        }

        [Test]
        public async Task EnsureAsync_downloads_verifies_and_caches_a_matching_release()
        {
            var executableName = UvPlatform.ExecutableName(this.target);
            var archiveBytes = BuildArchiveContaining(this.target, executableName, Encoding.UTF8.GetBytes("fake uv"));
            var sha256 = Convert.ToHexStringLower(SHA256.HashData(archiveBytes));
            var assetName = UvPlatform.AssetName(this.target);

            var releaseJson = JsonSerializer.Serialize(new
            {
                assets = new object[]
                {
                    new { name = assetName, browser_download_url = "https://example.com/download/" + assetName },
                    new { name = assetName + ".sha256", browser_download_url = "https://example.com/download/" + assetName + ".sha256" },
                },
            });

            var handler = new StubHttpMessageHandler();
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releaseJson) });
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{sha256} *{assetName}") });
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(archiveBytes) });

            var provisioner = new UvProvisioner(new HttpClient(handler), this.cacheRoot);

            var executable = await provisioner.EnsureAsync();

            Assert.That(executable, Is.Not.Null);
            Assert.That(File.Exists(executable!.FullName));
            Assert.That(File.Exists(provisioner.OkMarker(UvProvisioner.PinnedVersion, this.target).FullName));
        }

        [Test]
        public async Task EnsureAsync_returns_null_and_does_not_cache_on_a_checksum_mismatch()
        {
            var executableName = UvPlatform.ExecutableName(this.target);
            var archiveBytes = BuildArchiveContaining(this.target, executableName, Encoding.UTF8.GetBytes("fake uv"));
            var assetName = UvPlatform.AssetName(this.target);

            var releaseJson = JsonSerializer.Serialize(new
            {
                assets = new object[]
                {
                    new { name = assetName, browser_download_url = "https://example.com/download/" + assetName },
                    new { name = assetName + ".sha256", browser_download_url = "https://example.com/download/" + assetName + ".sha256" },
                },
            });

            var handler = new StubHttpMessageHandler();
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releaseJson) });
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent($"{new string('0', 64)} *{assetName}") }); // deliberately wrong
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(archiveBytes) });

            var provisioner = new UvProvisioner(new HttpClient(handler), this.cacheRoot);

            var executable = await provisioner.EnsureAsync();

            Assert.That(executable, Is.Null);
            Assert.That(provisioner.OkMarker(UvProvisioner.PinnedVersion, this.target).Exists, Is.False);
        }

        [Test]
        public async Task EnsureAsync_returns_null_when_the_release_is_not_found()
        {
            var handler = new StubHttpMessageHandler();
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

            var provisioner = new UvProvisioner(new HttpClient(handler), this.cacheRoot);

            var executable = await provisioner.EnsureAsync();

            Assert.That(executable, Is.Null);
        }

        [Test]
        public async Task EnsureAsync_returns_null_when_no_asset_matches_this_platform()
        {
            var releaseJson = JsonSerializer.Serialize(new
            {
                assets = new object[]
                {
                    new { name = "uv-some-other-target.zip", browser_download_url = "https://example.com/x.zip" },
                },
            });

            var handler = new StubHttpMessageHandler();
            handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(releaseJson) });

            var provisioner = new UvProvisioner(new HttpClient(handler), this.cacheRoot);

            var executable = await provisioner.EnsureAsync();

            Assert.That(executable, Is.Null);
        }

        [Test]
        public async Task EnsureAsync_never_throws_when_the_http_client_itself_throws()
        {
            var handler = new StubHttpMessageHandler();
            handler.Enqueue(_ => throw new HttpRequestException("network is down"));

            var provisioner = new UvProvisioner(new HttpClient(handler), this.cacheRoot);

            FileInfo? executable = null;
            await Assert.DoesNotThrowAsync(async () => executable = await provisioner.EnsureAsync());
            Assert.That(executable, Is.Null);
        }

        [Test]
        public async Task EnsureAsync_reuses_the_cache_without_a_network_call_once_verified()
        {
            var binDirectory = new UvProvisioner(new HttpClient(new StubHttpMessageHandler()), this.cacheRoot)
                .BinDirectory(UvProvisioner.PinnedVersion, this.target);
            binDirectory.Create();
            var executableName = UvPlatform.ExecutableName(this.target);
            await File.WriteAllTextAsync(Path.Combine(binDirectory.FullName, executableName), "already here");
            await File.WriteAllTextAsync(Path.Combine(binDirectory.FullName, ".ok"), "verified");

            var handler = new StubHttpMessageHandler(); // no responses enqueued - any HTTP call would throw
            var provisioner = new UvProvisioner(new HttpClient(handler), this.cacheRoot);

            var executable = await provisioner.EnsureAsync();

            Assert.That(executable, Is.Not.Null);
            Assert.That(executable!.FullName, Is.EqualTo(Path.Combine(binDirectory.FullName, executableName)));
        }
    }
}
