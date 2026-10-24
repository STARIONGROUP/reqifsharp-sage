// -------------------------------------------------------------------------------------------------
// <copyright file="RepositoryLayoutTests.cs" company="Starion Group S.A.">
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
    using System.Text;

    [TestFixture]
    public class RepositoryLayoutTests
    {
        private static readonly RepositoryLayout Layout = new(Path.Combine(Path.GetTempPath(), "reqifsharp-sage-layout"));

        [TestCase("1.2")]
        [TestCase("1.7")]
        [TestCase("10.1.2")]
        [TestCase("1.0.1-beta_2")]
        public void ValidateVersion_accepts_plain_version_names(string version)
        {
            Assert.That(RepositoryLayout.ValidateVersion(version), Is.EqualTo(version));
        }

        [TestCase("")]
        [TestCase("..")]
        [TestCase("../../docs")]
        [TestCase("1..2")]
        [TestCase(".hidden")]
        [TestCase("C:\\")]
        [TestCase("/")]
        [TestCase("1.2/../..")]
        [TestCase("a b")]
        public void ValidateVersion_rejects_anything_that_could_leave_the_layout(string version)
        {
            Assert.That(() => RepositoryLayout.ValidateVersion(version), Throws.ArgumentException);
        }

        [Test]
        public void Every_versioned_directory_rejects_a_path_like_version()
        {
            Assert.That(() => Layout.SourcesDirectoryFor(".."), Throws.ArgumentException);
            Assert.That(() => Layout.KnowledgeDirectoryFor(".."), Throws.ArgumentException);
            Assert.That(() => Layout.GuideSourcesDirectoryFor(".."), Throws.ArgumentException);
            Assert.That(() => Layout.GuideKnowledgeDirectoryFor(".."), Throws.ArgumentException);
            Assert.That(() => Layout.ApiKnowledgeDirectoryFor(".."), Throws.ArgumentException);
        }

        [Test]
        public void Import_with_a_path_like_version_is_refused_and_writes_nothing()
        {
            var root = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(root);
            try
            {
                var pdf = Path.Combine(root, "guide.pdf");
                File.WriteAllBytes(pdf, Encoding.ASCII.GetBytes("%PDF-1.4 fake"));
                var layout = new RepositoryLayout(Path.Combine(root, "repo"));

                Assert.That(() => GuideImporter.Import(layout, pdf, "../../docs"), Throws.TypeOf<ArgumentException>());
                Assert.That(Directory.Exists(Path.Combine(root, "docs")), Is.False);
                Assert.That(Directory.Exists(layout.RepositoryRoot), Is.False);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
