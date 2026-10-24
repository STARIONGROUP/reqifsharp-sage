// -------------------------------------------------------------------------------------------------
// <copyright file="SourceFetcherTests.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Knowledge.Tests
{
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class SourceFetcherTests
    {
        private string tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.tempDirectory))
            {
                Directory.Delete(this.tempDirectory, recursive: true);
            }
        }

        [Test]
        public async Task FetchAsync_downloads_every_file_and_writes_a_manifest()
        {
            var handler = new StubHttpMessageHandler();
            handler.EnqueueSuccess(TestDescriptors.PdfBytes);
            handler.EnqueueSuccess(TestDescriptors.CmofBytes);
            handler.EnqueueSuccess(TestDescriptors.XsdBytes);
            handler.EnqueueSuccess(TestDescriptors.DriverBytes);
            var target = Path.Combine(this.tempDirectory, "sources", "1.2");

            var entries = await new SourceFetcher(new HttpClient(handler)).FetchAsync(TestDescriptors.Fake, target, includeSpecs: true, fromDirectory: null);

            Assert.That(entries.Select(e => e.File), Is.EqualTo(new[] { "specs/ReqIF-1.2.pdf", "cmof/reqif.cmof", "schema/reqif.xsd", "schema/driver.xsd" }));
            Assert.That(entries.All(e => e.Origin == "download" && e.MatchesPublished), Is.True);
            Assert.That(File.ReadAllBytes(Path.Combine(target, "cmof", "reqif.cmof")), Is.EqualTo(TestDescriptors.CmofBytes));
            Assert.That(File.Exists(Path.Combine(target, "fetch-manifest.json")), Is.True);
        }

        [Test]
        public async Task FetchAsync_without_specs_skips_the_pdf()
        {
            var handler = new StubHttpMessageHandler();
            handler.EnqueueSuccess(TestDescriptors.CmofBytes);
            handler.EnqueueSuccess(TestDescriptors.XsdBytes);
            handler.EnqueueSuccess(TestDescriptors.DriverBytes);

            var entries = await new SourceFetcher(new HttpClient(handler)).FetchAsync(TestDescriptors.Fake, this.tempDirectory, includeSpecs: false, fromDirectory: null);

            Assert.That(entries, Has.Count.EqualTo(3));
            Assert.That(handler.RequestCount, Is.EqualTo(3));
        }

        [Test]
        public async Task FetchAsync_from_a_directory_recognises_files_by_hash_under_any_name_and_downloads_the_rest()
        {
            var from = Path.Combine(this.tempDirectory, "from");
            Directory.CreateDirectory(from);
            File.WriteAllBytes(Path.Combine(from, "dtc-10-12-13.cmof.xml"), TestDescriptors.CmofBytes);
            File.WriteAllBytes(Path.Combine(from, "formal-16-07-01.pdf"), TestDescriptors.PdfBytes);
            File.WriteAllBytes(Path.Combine(from, "reqif.xsd"), Encoding.UTF8.GetBytes("<xsd:schema xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"> </xsd:schema>"));

            var handler = new StubHttpMessageHandler();
            handler.EnqueueSuccess(TestDescriptors.DriverBytes);
            var target = Path.Combine(this.tempDirectory, "sources", "1.2");

            var entries = await new SourceFetcher(new HttpClient(handler)).FetchAsync(TestDescriptors.Fake, target, includeSpecs: true, fromDirectory: from);

            var byFile = entries.ToDictionary(e => e.File);
            Assert.That(byFile["cmof/reqif.cmof"].Origin, Is.EqualTo("local"));
            Assert.That(byFile["cmof/reqif.cmof"].MatchesPublished, Is.True);
            Assert.That(byFile["specs/ReqIF-1.2.pdf"].Origin, Is.EqualTo("local"));
            Assert.That(byFile["schema/reqif.xsd"].Origin, Is.EqualTo("local"), "matched by name");
            Assert.That(byFile["schema/reqif.xsd"].MatchesPublished, Is.False, "the local copy differs from the published file");
            Assert.That(byFile["schema/driver.xsd"].Origin, Is.EqualTo("download"));
            Assert.That(handler.RequestCount, Is.EqualTo(1));
        }

        [Test]
        public void FetchAsync_from_a_missing_directory_throws()
        {
            var fetcher = new SourceFetcher(new HttpClient(new StubHttpMessageHandler()));

            Assert.That(
                async () => await fetcher.FetchAsync(TestDescriptors.Fake, this.tempDirectory, true, Path.Combine(this.tempDirectory, "missing")),
                Throws.TypeOf<DirectoryNotFoundException>());
        }

        [Test]
        public void FetchAsync_rejects_an_html_error_page_served_instead_of_the_pdf()
        {
            var handler = new StubHttpMessageHandler();
            handler.EnqueueSuccess(Encoding.UTF8.GetBytes("<!DOCTYPE html><html>Not found</html>"));

            Assert.That(
                async () => await new SourceFetcher(new HttpClient(handler)).FetchAsync(TestDescriptors.Fake, this.tempDirectory, true, null),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void FetchAsync_rejects_a_non_xml_response_for_the_metamodel()
        {
            var handler = new StubHttpMessageHandler();
            handler.EnqueueSuccess(Encoding.UTF8.GetBytes("{\"error\":\"not found\"}"));

            Assert.That(
                async () => await new SourceFetcher(new HttpClient(handler)).FetchAsync(TestDescriptors.Fake, this.tempDirectory, false, null),
                Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void FetchAsync_surfaces_an_http_failure_after_retrying()
        {
            var handler = new StubHttpMessageHandler();
            for (var attempt = 0; attempt < 4; attempt++)
            {
                handler.Enqueue(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            Assert.That(
                async () => await new SourceFetcher(new HttpClient(handler)).FetchAsync(TestDescriptors.Fake, this.tempDirectory, false, null),
                Throws.TypeOf<HttpRequestException>());
            Assert.That(handler.RequestCount, Is.EqualTo(4));
        }
    }
}
