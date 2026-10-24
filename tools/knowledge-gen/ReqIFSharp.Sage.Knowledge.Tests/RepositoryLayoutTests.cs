// -------------------------------------------------------------------------------------------------
// <copyright file="RepositoryLayoutTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
