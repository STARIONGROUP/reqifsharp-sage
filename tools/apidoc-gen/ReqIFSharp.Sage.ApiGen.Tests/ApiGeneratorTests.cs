// -------------------------------------------------------------------------------------------------
// <copyright file="ApiGeneratorTests.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.ApiGen.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    using ReqIFSharp.Sage.ApiGen;

    [TestFixture]
    public class ApiGeneratorTests
    {
        private string firstDirectory = string.Empty;

        private string secondDirectory = string.Empty;

        private ApiGenerationResult firstResult = null!;

        [OneTimeSetUp]
        public void GenerateTwice()
        {
            var root = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-apigen-tests", Guid.NewGuid().ToString("N"));
            this.firstDirectory = Path.Combine(root, "first");
            this.secondDirectory = Path.Combine(root, "second");

            this.firstResult = ApiGenerator.Generate(this.firstDirectory);
            ApiGenerator.Generate(this.secondDirectory);
        }

        [OneTimeTearDown]
        public void DeleteOutput()
        {
            var root = Path.GetDirectoryName(this.firstDirectory);
            if (root is not null && Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }

        [Test]
        public void Result_reports_versions_type_count_and_no_warnings()
        {
            Assert.Multiple(() =>
            {
                Assert.That(this.firstResult.ReqIFSharpVersion, Is.EqualTo("10.1.2"));
                Assert.That(this.firstResult.ExtensionsVersion, Is.EqualTo("5.2.2"));
                Assert.That(this.firstResult.OutputDirectory, Is.EqualTo(this.firstDirectory));
                Assert.That(this.firstResult.Warnings, Is.Empty);
                Assert.That(Directory.GetFiles(Path.Combine(this.firstDirectory, "types"), "*.md"), Has.Length.EqualTo(this.firstResult.TypeCount));
                Assert.That(this.firstResult.FileCount, Is.EqualTo(this.firstResult.TypeCount + 5));
            });
        }

        [Test]
        public void Generating_twice_yields_byte_identical_output()
        {
            var firstFiles = Directory.GetFiles(this.firstDirectory, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(this.firstDirectory, path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            var secondFiles = Directory.GetFiles(this.secondDirectory, "*", SearchOption.AllDirectories)
                .Select(path => Path.GetRelativePath(this.secondDirectory, path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();

            Assert.That(secondFiles, Is.EqualTo(firstFiles));

            Assert.Multiple(() =>
            {
                foreach (var file in firstFiles)
                {
                    var first = File.ReadAllBytes(Path.Combine(this.firstDirectory, file));
                    var second = File.ReadAllBytes(Path.Combine(this.secondDirectory, file));
                    Assert.That(second, Is.EqualTo(first), file);
                }
            });
        }

        [Test]
        public void Output_uses_lf_line_endings_without_bom_or_machine_paths()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            Assert.Multiple(() =>
            {
                foreach (var path in Directory.GetFiles(this.firstDirectory, "*", SearchOption.AllDirectories))
                {
                    var bytes = File.ReadAllBytes(path);
                    var text = File.ReadAllText(path);
                    Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }), path);
                    Assert.That(text, Does.Not.Contain("\r"), path);
                    Assert.That(text, Does.Not.Contain(userProfile).IgnoreCase, path);
                }
            });
        }

        [Test]
        public void Index_json_rows_are_sorted_and_point_at_existing_pages()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(this.firstDirectory, "index.json")));
            var rows = document.RootElement.EnumerateArray().ToList();
            var fullNames = rows.Select(row => row.GetProperty("fullName").GetString()!).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(rows, Has.Count.EqualTo(this.firstResult.TypeCount));
                Assert.That(fullNames, Is.EqualTo(fullNames.OrderBy(name => name, StringComparer.Ordinal).ToList()));

                foreach (var row in rows)
                {
                    Assert.That(File.Exists(Path.Combine(this.firstDirectory, row.GetProperty("file").GetString()!)), Is.True);
                }

                var specObject = rows.Single(row => row.GetProperty("fullName").GetString() == "ReqIFSharp.SpecObject");
                Assert.That(specObject.GetProperty("reqifClass").GetString(), Is.EqualTo("SpecObject"));
                Assert.That(specObject.GetProperty("xmlElement").GetString(), Is.EqualTo("SPEC-OBJECT"));
            });
        }

        [Test]
        public void Mapping_json_maps_metaclasses_to_types_and_elements()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(this.firstDirectory, "mapping.json")));
            var root = document.RootElement;
            var entries = root.GetProperty("entries").EnumerateArray().ToList();
            var specObject = entries.Single(entry => entry.GetProperty("reqifClass").GetString() == "SpecObject");
            var identifiable = entries.Single(entry => entry.GetProperty("reqifClass").GetString() == "Identifiable");

            Assert.Multiple(() =>
            {
                Assert.That(root.GetProperty("reqifSharpVersion").GetString(), Is.EqualTo("10.1.2"));
                Assert.That(root.GetProperty("extensionsVersion").GetString(), Is.EqualTo("5.2.2"));
                Assert.That(specObject.GetProperty("csharpType").GetString(), Is.EqualTo("ReqIFSharp.SpecObject"));
                Assert.That(specObject.GetProperty("xmlElement").GetString(), Is.EqualTo("SPEC-OBJECT"));
                Assert.That(specObject.GetProperty("properties").EnumerateArray().Select(p => p.GetProperty("reqifFeature").GetString()), Does.Contain("type"));
                Assert.That(identifiable.GetProperty("xmlElement").ValueKind, Is.EqualTo(JsonValueKind.Null));
                Assert.That(identifiable.GetProperty("isAbstract").GetBoolean(), Is.True);
            });
        }

        [Test]
        public void Manifest_lists_both_packages_and_the_generator()
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(this.firstDirectory, "manifest.json")));
            var packages = document.RootElement.GetProperty("packages").EnumerateArray().ToList();

            Assert.Multiple(() =>
            {
                Assert.That(document.RootElement.GetProperty("generator").GetString(), Is.EqualTo("reqifsharp-sage"));
                Assert.That(packages.Select(p => p.GetProperty("id").GetString() + " " + p.GetProperty("version").GetString()), Is.EqualTo(new[] { "ReqIFSharp 10.1.2", "ReqIFSharp.Extensions 5.2.2" }));
                Assert.That(packages.All(p => !string.IsNullOrEmpty(p.GetProperty("assemblyVersion").GetString())), Is.True);
            });
        }

        [Test]
        public void SpecObject_page_has_front_matter_mapping_and_extension_methods()
        {
            var page = File.ReadAllText(Path.Combine(this.firstDirectory, "types", "ReqIFSharp.SpecObject.md"));

            Assert.Multiple(() =>
            {
                Assert.That(page, Does.StartWith("---\nname: \"SpecObject\"\nfullName: \"ReqIFSharp.SpecObject\"\n"));
                Assert.That(page, Does.Contain("reqifClass: \"SpecObject\"\n"));
                Assert.That(page, Does.Contain("xmlElement: \"SPEC-OBJECT\"\n"));
                Assert.That(page, Does.Contain("baseType: \"ReqIFSharp.SpecElementWithAttributes\"\n"));
                Assert.That(page, Does.Contain("## ReqIF mapping"));
                Assert.That(page, Does.Contain("## Extension methods available"));
                Assert.That(page, Does.Contain("QueryContainerSpecifications"));
            });
        }

        [Test]
        public void ReqIFDeserializer_page_lists_deserialize_with_readable_generic_types()
        {
            var page = File.ReadAllText(Path.Combine(this.firstDirectory, "types", "ReqIFSharp.ReqIFDeserializer.md"));

            Assert.Multiple(() =>
            {
                Assert.That(page, Does.Contain("### Deserialize(string, bool, ValidationEventHandler)"));
                Assert.That(page, Does.Contain("public Task<IEnumerable<ReqIF>> DeserializeAsync(string fileUri, CancellationToken token, bool validate = false, ValidationEventHandler validationEventHandler = null)"));
                Assert.That(page, Does.Contain("**Returns:** `IEnumerable<ReqIF>`"));
            });
        }

        [Test]
        public void Extension_class_page_marks_extension_methods_with_the_extended_type()
        {
            var page = File.ReadAllText(Path.Combine(this.firstDirectory, "types", "ReqIFSharp.Extensions.ReqIFExtensions.SpecObjectExtensions.md"));

            Assert.Multiple(() =>
            {
                Assert.That(page, Does.Contain("isStatic: true"));
                Assert.That(page, Does.Contain("### QueryContainerSpecifications(this SpecObject) (extension)"));
                Assert.That(page, Does.Contain("Extension method on [`SpecObject`](ReqIFSharp.SpecObject.md)."));
            });
        }

        [Test]
        [Explicit("Writes the real generator output to a scratch directory for manual inspection.")]
        public void Generate_into_the_directory_named_by_the_environment()
        {
            var directory = Environment.GetEnvironmentVariable("REQIFSHARP_SAGE_APIGEN_OUT");
            Assume.That(directory, Is.Not.Null.And.Not.Empty);

            var result = ApiGenerator.Generate(directory!);
            TestContext.Out.WriteLine($"{result.TypeCount} types, ReqIFSharp {result.ReqIFSharpVersion}, Extensions {result.ExtensionsVersion}, {result.Warnings.Count} warnings");
            foreach (var warning in result.Warnings)
            {
                TestContext.Out.WriteLine(warning);
            }
        }
    }
}
