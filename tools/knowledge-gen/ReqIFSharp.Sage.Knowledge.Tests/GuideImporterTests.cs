// -------------------------------------------------------------------------------------------------
// <copyright file="GuideImporterTests.cs" company="Starion Group S.A.">
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
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    [TestFixture]
    public class GuideImporterTests
    {
        private string tempDirectory = null!;

        private RepositoryLayout layout = null!;

        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(this.tempDirectory);
            this.layout = new RepositoryLayout(Path.Combine(this.tempDirectory, "repo"));
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(this.tempDirectory, recursive: true);
        }

        [Test]
        public void Import_of_an_unknown_pdf_with_a_version_stores_it_and_warns()
        {
            var pdf = this.WritePdf("my-copy.pdf", "%PDF-1.4 not the real guide");

            var result = GuideImporter.Import(this.layout, pdf, "1.7");

            Assert.That(result.Version, Is.EqualTo("1.7"));
            Assert.That(result.IsKnownRelease, Is.False);
            Assert.That(result.Warning, Does.Contain("not a known release"));
            Assert.That(result.StoredPath, Does.EndWith("PSI-ReqIF-Implementation-Guide-1.7.pdf"));
            Assert.That(File.Exists(result.StoredPath), Is.True);
            Assert.That(GuideImporter.ImportedPdfPathFor(this.layout, "1.7"), Is.EqualTo(result.StoredPath));
            Assert.That(new InstalledVersionsStore(this.layout.KnowledgeRoot).Load().Guides.Single().Imported, Is.True);
        }

        [Test]
        public void Import_of_an_unknown_pdf_without_a_version_is_refused()
        {
            var pdf = this.WritePdf("my-copy.pdf", "%PDF-1.4 not the real guide");

            Assert.That(() => GuideImporter.Import(this.layout, pdf, null), Throws.TypeOf<ArgumentException>());
        }

        [Test]
        public void Import_of_a_file_that_is_not_a_pdf_is_refused()
        {
            var notPdf = this.WritePdf("guide.pdf", "<html>");

            Assert.That(() => GuideImporter.Import(this.layout, notPdf, "1.7"), Throws.TypeOf<InvalidDataException>());
        }

        [Test]
        public void Import_of_a_missing_file_is_refused()
        {
            Assert.That(() => GuideImporter.Import(this.layout, Path.Combine(this.tempDirectory, "missing.pdf"), "1.7"), Throws.TypeOf<FileNotFoundException>());
        }

        [Test]
        public void ImportedPdfPathFor_a_version_that_was_never_imported_is_null()
        {
            Assert.That(GuideImporter.ImportedPdfPathFor(this.layout, "1.7"), Is.Null);
        }

        [Test]
        public void The_known_guide_registry_resolves_by_version_and_hash()
        {
            var current = KnownGuideVersions.Current;

            Assert.That(KnownGuideVersions.Find(current.Version), Is.SameAs(current));
            Assert.That(KnownGuideVersions.FindBySha256(current.Sha256.ToUpperInvariant()), Is.SameAs(current));
            Assert.That(KnownGuideVersions.FindBySha256("00"), Is.Null);
        }

        private string WritePdf(string name, string content)
        {
            var path = Path.Combine(this.tempDirectory, name);
            File.WriteAllBytes(path, Encoding.ASCII.GetBytes(content));
            return path;
        }
    }
}
