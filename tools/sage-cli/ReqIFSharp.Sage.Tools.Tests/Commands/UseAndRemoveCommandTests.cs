// -------------------------------------------------------------------------------------------------
// <copyright file="UseAndRemoveCommandTests.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Tests.Commands
{
    using System.IO;

    using ReqIFSharp.Sage.Knowledge;
    using ReqIFSharp.Sage.Tools.Commands;

    [TestFixture]
    public class UseAndRemoveCommandTests
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
        public void Use_returns_1_when_the_version_has_not_been_generated()
        {
            new InstalledVersionsStore(this.layout.KnowledgeRoot).MarkFetched("1.2", setAsDefault: false);

            var exitCode = UseCommand.Build().Parse(["--repository-root", this.repositoryRoot, "--version", "1.2"]).Invoke();

            Assert.That(exitCode, Is.EqualTo(1));
        }

        [Test]
        public void Use_returns_0_and_updates_the_default_once_generated()
        {
            var store = new InstalledVersionsStore(this.layout.KnowledgeRoot);
            store.MarkFetched("1.2", setAsDefault: false);
            store.MarkGenerated("1.2", specGenerated: false);

            var exitCode = UseCommand.Build().Parse(["--repository-root", this.repositoryRoot, "--version", "1.2"]).Invoke();

            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(store.Load().Default, Is.EqualTo("1.2"));
        }

        [Test]
        public void Remove_returns_1_on_the_current_default_without_force()
        {
            new InstalledVersionsStore(this.layout.KnowledgeRoot).MarkFetched("1.2", setAsDefault: true);

            var exitCode = RemoveCommand.Build().Parse(["--repository-root", this.repositoryRoot, "--version", "1.2"]).Invoke();

            Assert.That(exitCode, Is.EqualTo(1));
        }

        [Test]
        public void Remove_with_force_deletes_the_sources_and_knowledge_directories()
        {
            new InstalledVersionsStore(this.layout.KnowledgeRoot).MarkFetched("1.2", setAsDefault: true);
            Directory.CreateDirectory(this.layout.SourcesDirectoryFor("1.2"));
            Directory.CreateDirectory(this.layout.KnowledgeDirectoryFor("1.2"));

            var exitCode = RemoveCommand.Build().Parse(["--repository-root", this.repositoryRoot, "--version", "1.2", "--force"]).Invoke();

            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(Directory.Exists(this.layout.SourcesDirectoryFor("1.2")), Is.False);
            Assert.That(Directory.Exists(this.layout.KnowledgeDirectoryFor("1.2")), Is.False);
        }

        [Test]
        public void Remove_guide_deletes_the_imported_pdf_and_its_extracted_text_but_not_the_reqif_version()
        {
            var store = new InstalledVersionsStore(this.layout.KnowledgeRoot);
            store.MarkFetched("1.2", setAsDefault: true);
            store.MarkGuideImported("1.7");
            Directory.CreateDirectory(this.layout.GuideSourcesDirectoryFor("1.7"));
            Directory.CreateDirectory(this.layout.GuideKnowledgeDirectoryFor("1.7"));

            var exitCode = RemoveCommand.Build().Parse(["--repository-root", this.repositoryRoot, "--guide", "1.7"]).Invoke();

            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(Directory.Exists(this.layout.GuideSourcesDirectoryFor("1.7")), Is.False);
            Assert.That(Directory.Exists(this.layout.GuideKnowledgeDirectoryFor("1.7")), Is.False);
            Assert.That(store.Load().Guides, Is.Empty);
            Assert.That(store.Load().Versions, Has.Count.EqualTo(1));
        }

        [TestCase("..")]
        [TestCase("")]
        [TestCase("1.6")]
        public void Remove_guide_that_is_not_imported_returns_1_and_deletes_nothing(string guide)
        {
            var store = new InstalledVersionsStore(this.layout.KnowledgeRoot);
            store.MarkGuideImported("1.7");
            Directory.CreateDirectory(this.layout.SourcesDirectoryFor("1.2"));
            Directory.CreateDirectory(this.layout.KnowledgeDirectoryFor("1.2"));
            Directory.CreateDirectory(this.layout.GuideSourcesDirectoryFor("1.7"));

            var exitCode = RemoveCommand.Build().Parse(["--repository-root", this.repositoryRoot, "--guide", guide]).Invoke();

            Assert.That(exitCode, Is.EqualTo(1));
            Assert.That(Directory.Exists(this.layout.SourcesDirectoryFor("1.2")), Is.True);
            Assert.That(Directory.Exists(this.layout.KnowledgeDirectoryFor("1.2")), Is.True);
            Assert.That(Directory.Exists(this.layout.GuideSourcesDirectoryFor("1.7")), Is.True);
            Assert.That(store.Load().Guides, Has.Count.EqualTo(1));
        }
    }
}
