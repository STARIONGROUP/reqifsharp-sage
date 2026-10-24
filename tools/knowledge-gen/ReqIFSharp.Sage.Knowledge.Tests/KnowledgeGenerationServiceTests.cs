// -------------------------------------------------------------------------------------------------
// <copyright file="KnowledgeGenerationServiceTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge.Tests
{
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    [TestFixture]
    public class KnowledgeGenerationServiceTests
    {
        private string repositoryRoot = null!;

        private RepositoryLayout layout = null!;

        [SetUp]
        public void SetUp()
        {
            this.repositoryRoot = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
            this.layout = new RepositoryLayout(this.repositoryRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.repositoryRoot))
            {
                Directory.Delete(this.repositoryRoot, recursive: true);
            }
        }

        [Test]
        public async Task FetchAsync_stores_the_sources_and_records_the_first_version_as_default()
        {
            var handler = new StubHttpMessageHandler();
            handler.EnqueueSuccess(TestDescriptors.CmofBytes);
            handler.EnqueueSuccess(TestDescriptors.XsdBytes);
            handler.EnqueueSuccess(TestDescriptors.DriverBytes);

            await CreateService(handler).FetchAsync(this.layout, TestDescriptors.Fake, includeSpecs: false, fromDirectory: null, setAsDefault: false);

            var manifest = new InstalledVersionsStore(this.layout.KnowledgeRoot).Load();
            Assert.That(manifest.Default, Is.EqualTo("1.2"));
            Assert.That(manifest.Versions.Single().Fetched, Is.True);
            Assert.That(File.Exists(Path.Combine(this.layout.SourcesDirectoryFor("1.2"), "cmof", "reqif.cmof")), Is.True);
        }

        [Test]
        public void GenerateAsync_before_fetch_explains_what_to_run()
        {
            Assert.That(
                async () => await CreateService(new StubHttpMessageHandler()).GenerateAsync(this.layout, TestDescriptors.Fake),
                Throws.TypeOf<FileNotFoundException>().With.Message.Contains("fetch --version 1.2"));
        }

        [Test]
        public async Task GenerateAsync_writes_metamodel_schema_api_and_datapackage_without_a_spec_pdf()
        {
            this.StoreToySources();

            var outcome = await CreateService(new StubHttpMessageHandler()).GenerateAsync(this.layout, TestDescriptors.Fake);

            var knowledge = this.layout.KnowledgeDirectoryFor("1.2");
            Assert.That(outcome.Metamodel.ClassCount, Is.EqualTo(3));
            Assert.That(outcome.Specification.Succeeded, Is.False, "no PDF was stored in this test");
            Assert.That(outcome.Guides, Is.Empty);
            Assert.That(File.Exists(Path.Combine(knowledge, "metamodel", "elements", "WidgetBox.md")), Is.True);
            Assert.That(File.Exists(Path.Combine(knowledge, "schema", "index.json")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(knowledge, "datapackage.json")), Does.Not.Contain("spec-clause-index"));

            Assert.That(outcome.Api.Succeeded, Is.True, outcome.Api.SkippedReason);
            Assert.That(File.Exists(Path.Combine(this.layout.ApiKnowledgeDirectoryFor(outcome.Api.ReqIFSharpVersion!), "index.json")), Is.True);
            Assert.That(Directory.Exists(Path.Combine(this.layout.KnowledgeRoot, "reqifsharp", ".staging")), Is.False);

            var manifest = new InstalledVersionsStore(this.layout.KnowledgeRoot).Load();
            Assert.That(manifest.Versions.Single().Generated, Is.True);
            Assert.That(manifest.Versions.Single().SpecGenerated, Is.False);
            Assert.That(manifest.Api!.ReqIFSharpVersion, Is.EqualTo(outcome.Api.ReqIFSharpVersion));
        }

        [Test]
        public async Task GenerateAsync_attempts_every_imported_guide_and_does_not_fail_when_extraction_is_skipped()
        {
            this.StoreToySources();
            var pdf = Path.Combine(this.repositoryRoot, "guide.pdf");
            File.WriteAllBytes(pdf, Encoding.ASCII.GetBytes("%PDF-1.4 fake guide"));
            GuideImporter.Import(this.layout, pdf, "1.7");

            var outcome = await CreateService(new StubHttpMessageHandler()).GenerateAsync(this.layout, TestDescriptors.Fake);

            var guide = outcome.Guides.Single();
            Assert.That(guide.Version, Is.EqualTo("1.7"));
            Assert.That(guide.Extraction.Succeeded, Is.False, "no Python is available in this test");
            Assert.That(new InstalledVersionsStore(this.layout.KnowledgeRoot).Load().Guides.Single().Generated, Is.False);
        }

        private static KnowledgeGenerationService CreateService(StubHttpMessageHandler handler)
        {
            return new KnowledgeGenerationService(
                new SourceFetcher(new HttpClient(handler)),
                new PythonSpecExtractRunner(FakeProcessRunner.NotFound(), FakeUvProvisioner.Unavailable()));
        }

        private void StoreToySources()
        {
            var descriptor = TestDescriptors.Fake;
            var cmof = this.layout.SourcePathFor("1.2", descriptor.FileFor(SourceFileRole.Metamodel));
            var xsd = this.layout.SourcePathFor("1.2", descriptor.FileFor(SourceFileRole.Schema));
            Directory.CreateDirectory(Path.GetDirectoryName(cmof)!);
            Directory.CreateDirectory(Path.GetDirectoryName(xsd)!);
            File.Copy(TestDescriptors.ToyFixture("reqif.cmof"), cmof);
            File.Copy(TestDescriptors.ToyFixture("reqif.xsd"), xsd);
        }
    }
}
