// -------------------------------------------------------------------------------------------------
// <copyright file="InstalledVersionsStoreTests.cs" company="Starion Group S.A.">
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

    [TestFixture]
    public class InstalledVersionsStoreTests
    {
        private string tempDirectory = null!;
        private InstalledVersionsStore store = null!;

        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
            this.store = new InstalledVersionsStore(this.tempDirectory);
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
        public void Load_with_no_file_yet_returns_an_empty_manifest()
        {
            var manifest = this.store.Load();

            Assert.That(manifest.Default, Is.Null);
            Assert.That(manifest.Versions, Is.Empty);
        }

        [Test]
        public void MarkFetched_on_the_first_version_sets_it_as_default()
        {
            this.store.MarkFetched("1.2", setAsDefault: false);

            var manifest = this.store.Load();

            Assert.That(manifest.Default, Is.EqualTo("1.2"));
            Assert.That(manifest.Versions.Single().Fetched, Is.True);
            Assert.That(manifest.Versions.Single().Generated, Is.False);
        }

        [Test]
        public void MarkGenerated_preserves_the_fetched_flag_and_records_spec_generation()
        {
            this.store.MarkFetched("1.2", setAsDefault: true);

            this.store.MarkGenerated("1.2", specGenerated: true);

            var entry = this.store.Load().Versions.Single();
            Assert.That(entry.Fetched, Is.True);
            Assert.That(entry.Generated, Is.True);
            Assert.That(entry.SpecGenerated, Is.True);
        }

        [Test]
        public void SetDefault_throws_when_the_version_has_not_been_generated()
        {
            this.store.MarkFetched("1.2", setAsDefault: false);

            Assert.That(() => this.store.SetDefault("1.2"), Throws.InvalidOperationException);
        }

        [Test]
        public void SetDefault_succeeds_once_the_version_is_generated()
        {
            this.store.MarkFetched("1.2", setAsDefault: false);
            this.store.MarkGenerated("1.2", specGenerated: false);

            this.store.SetDefault("1.2");

            Assert.That(this.store.Load().Default, Is.EqualTo("1.2"));
        }

        [Test]
        public void Remove_drops_the_version_and_clears_the_default_when_it_was_the_default()
        {
            this.store.MarkFetched("1.2", setAsDefault: true);

            this.store.Remove("1.2");

            var manifest = this.store.Load();
            Assert.That(manifest.Versions, Is.Empty);
            Assert.That(manifest.Default, Is.Null);
        }

        [Test]
        public void MarkGuideImported_and_MarkGuideGenerated_round_trip_independently_of_reqif_versions()
        {
            this.store.MarkGuideImported("1.7");
            this.store.MarkGuideGenerated("1.7", succeeded: true);

            var manifest = this.store.Load();
            var entry = manifest.Guides.Single();
            Assert.That(entry.Version, Is.EqualTo("1.7"));
            Assert.That(entry.Imported, Is.True);
            Assert.That(entry.Generated, Is.True);
            Assert.That(manifest.Versions, Is.Empty);
        }

        [Test]
        public void MarkGuideGenerated_is_sticky_true_like_SpecGenerated()
        {
            this.store.MarkGuideGenerated("1.7", succeeded: true);
            this.store.MarkGuideGenerated("1.7", succeeded: false);

            Assert.That(this.store.Load().Guides.Single().Generated, Is.True, "a later failed attempt must not clear a prior success");
        }

        [Test]
        public void RemoveGuide_removes_only_that_guide()
        {
            this.store.MarkGuideImported("1.7");
            this.store.MarkFetched("1.2", setAsDefault: true);

            this.store.RemoveGuide("1.7");

            Assert.That(this.store.Load().Guides, Is.Empty);
            Assert.That(this.store.Load().Versions, Has.Count.EqualTo(1));
        }

        [Test]
        public void MarkApiGenerated_records_the_package_versions()
        {
            this.store.MarkApiGenerated(new InstalledApi("10.1.2", "5.2.2"));

            Assert.That(this.store.Load().Api, Is.EqualTo(new InstalledApi("10.1.2", "5.2.2")));
        }

        [Test]
        public void Load_coalesces_missing_guides_to_an_empty_list()
        {
            Directory.CreateDirectory(this.tempDirectory);
            File.WriteAllText(Path.Combine(this.tempDirectory, "installed.json"), """{"default":"1.2","versions":[]}""");

            Assert.That(() => this.store.Load(), Throws.Nothing);
            Assert.That(this.store.Load().Guides, Is.Empty);
            Assert.That(this.store.Load().Api, Is.Null);
        }
    }
}
