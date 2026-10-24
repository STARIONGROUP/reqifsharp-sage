// -------------------------------------------------------------------------------------------------
// <copyright file="DataPackageGeneratorTests.cs" company="Starion Group S.A.">
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
    using System.Text.Json;

    [TestFixture]
    public class DataPackageGeneratorTests
    {
        private string tempDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            this.tempDirectory = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(this.tempDirectory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(this.tempDirectory, recursive: true);
        }

        [Test]
        public void Generate_lists_only_the_indexes_that_exist()
        {
            this.WriteIndex("metamodel");

            using var document = JsonDocument.Parse(DataPackageGenerator.Generate(this.tempDirectory, TestDescriptors.Fake));
            var names = document.RootElement.GetProperty("resources").EnumerateArray().Select(r => r.GetProperty("name").GetString()).ToList();

            Assert.That(names, Is.EqualTo(new[] { "metamodel-index" }));
        }

        [Test]
        public void Generate_with_every_index_present_lists_all_three_with_a_primary_key()
        {
            this.WriteIndex("metamodel");
            this.WriteIndex("schema");
            this.WriteIndex("spec");

            using var document = JsonDocument.Parse(DataPackageGenerator.Generate(this.tempDirectory, TestDescriptors.Fake));
            var resources = document.RootElement.GetProperty("resources").EnumerateArray().ToList();

            Assert.That(resources.Select(r => r.GetProperty("name").GetString()), Is.EqualTo(new[] { "metamodel-index", "schema-index", "spec-clause-index" }));
            foreach (var resource in resources)
            {
                Assert.That(resource.GetProperty("profile").GetString(), Is.EqualTo("tabular-data-resource"));
                Assert.That(resource.GetProperty("schema").GetProperty("primaryKey").GetString(), Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void Generate_records_the_version_document_id_and_that_sources_are_not_redistributed()
        {
            var json = DataPackageGenerator.Generate(this.tempDirectory, TestDescriptors.Fake);

            Assert.That(json, Does.Contain("\"version\": \"1.2\""));
            Assert.That(json, Does.Contain("formal/16-07-01"));
            Assert.That(json, Does.Contain("Not redistributed"));
        }

        private void WriteIndex(string folder)
        {
            Directory.CreateDirectory(Path.Combine(this.tempDirectory, folder));
            File.WriteAllText(Path.Combine(this.tempDirectory, folder, "index.json"), "[]");
        }
    }
}
