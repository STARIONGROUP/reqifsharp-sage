// -------------------------------------------------------------------------------------------------
// <copyright file="MetamodelGeneratorTestFixture.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    using ReqIFSharp.Sage.MetamodelGen.Cmof;
    using ReqIFSharp.Sage.MetamodelGen.Xsd;

    [TestFixture]
    public class MetamodelGeneratorTestFixture
    {
        private string outputDirectory = null!;

        [SetUp]
        public void SetUp()
        {
            this.outputDirectory = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.outputDirectory))
            {
                Directory.Delete(this.outputDirectory, recursive: true);
            }
        }

        [Test]
        public void BindingOf_maps_features_to_xml_attributes_wrapper_elements_or_nothing()
        {
            var view = new MetamodelView(CmofReader.Read(Fixture.Cmof), XsdReader.Read(Fixture.Xsd));
            var widget = view.Model.Classes.Single(c => c.Name == "WidgetBox");
            var gadget = view.Model.Classes.Single(c => c.Name == "Gadget");
            var named = view.Model.Classes.Single(c => c.Name == "NamedThing");

            var longName = view.BindingOf(widget, named.OwnedAttributes.Single(p => p.Name == "longName"));
            var favourite = view.BindingOf(widget, widget.OwnedAttributes.Single(p => p.Name == "favouriteGadget"));
            var box = view.BindingOf(gadget, gadget.OwnedAttributes.Single(p => p.Name == "box"));

            Assert.That(longName.Kind, Is.EqualTo("attribute"));
            Assert.That(longName.XmlName, Is.EqualTo("LONG-NAME"));
            Assert.That(longName.Use, Is.EqualTo("optional"));
            Assert.That(favourite.Kind, Is.EqualTo("element"));
            Assert.That(favourite.Options.Single().Name, Is.EqualTo("GADGET-REF"));
            Assert.That(box.Kind, Is.EqualTo("none"));
        }

        [Test]
        public void BindingOf_an_abstract_class_feature_uses_a_concrete_descendant()
        {
            var view = new MetamodelView(CmofReader.Read(Fixture.Cmof), XsdReader.Read(Fixture.Xsd));
            var named = view.Model.Classes.Single(c => c.Name == "NamedThing");

            Assert.That(view.XmlElementOf(named), Is.Null);
            Assert.That(view.BindingOf(named, named.OwnedAttributes.Single(p => p.Name == "identifier")).XmlName, Is.EqualTo("IDENTIFIER"));
            Assert.That(view.AllDescendantsOf(named).Select(c => c.Name), Is.EqualTo(new[] { "Gadget", "WidgetBox" }));
        }

        [Test]
        public void UnboundSchemaItemsOf_lists_schema_items_without_a_feature()
        {
            var view = new MetamodelView(CmofReader.Read(Fixture.Cmof), XsdReader.Read(Fixture.Xsd));
            var widget = view.Model.Classes.Single(c => c.Name == "WidgetBox");

            Assert.That(view.UnboundSchemaItemsOf(widget, view.ComplexTypeOf(widget)!), Is.EqualTo(new[] { "xml:lang", "EXTRAS" }));
        }

        [Test]
        public void Generate_writes_element_pages_indexes_metamodel_json_and_schema_pages()
        {
            var result = MetamodelGenerator.Generate(Fixture.Cmof, Fixture.Xsd, Fixture.Xsd, this.outputDirectory);

            Assert.That(result.ClassCount, Is.EqualTo(3));
            Assert.That(result.PrimitiveTypeCount, Is.EqualTo(1));
            Assert.That(result.SchemaTypeCount, Is.EqualTo(3));

            var page = File.ReadAllText(Path.Combine(this.outputDirectory, "metamodel", "elements", "WidgetBox.md"));
            Assert.That(page, Does.StartWith("---\nname: \"WidgetBox\"\n"));
            Assert.That(page, Does.Contain("xmlElement: \"WIDGET-BOX\""));
            Assert.That(page, Does.Contain("| `gadgets` | [Gadget](Gadget.md) | `[0..*]` | composite, ordered | `box` | `<GADGETS>` → `<GADGET>` |"));
            Assert.That(page, Does.Contain("`favouredBy` (not navigable)"));
            Assert.That(page, Does.Contain("<WIDGET-BOX IDENTIFIER=\"...\">"));

            using var index = JsonDocument.Parse(File.ReadAllText(Path.Combine(this.outputDirectory, "metamodel", "index.json")));
            Assert.That(index.RootElement.EnumerateArray().Select(r => r.GetProperty("name").GetString()), Is.EqualTo(new[] { "Gadget", "ID", "NamedThing", "WidgetBox" }));

            using var metamodel = JsonDocument.Parse(File.ReadAllText(Path.Combine(this.outputDirectory, "metamodel", "metamodel.json")));
            var widgetNode = metamodel.RootElement.GetProperty("classes").EnumerateArray().Single(c => c.GetProperty("name").GetString() == "WidgetBox");
            Assert.That(widgetNode.GetProperty("inheritedAttributes").EnumerateArray().Select(f => f.GetProperty("name").GetString()), Is.EqualTo(new[] { "identifier", "longName" }));

            Assert.That(File.Exists(Path.Combine(this.outputDirectory, "schema", "types", "WIDGET-BOX.md")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(this.outputDirectory, "schema", "xhtml-subset.md")), Does.Contain("`toy.Block.class`"));
        }

        [Test]
        public void Generate_without_a_schema_writes_only_the_metamodel()
        {
            var result = MetamodelGenerator.Generate(Fixture.Cmof, null, null, this.outputDirectory);

            Assert.That(result.SchemaTypeCount, Is.Zero);
            Assert.That(Directory.Exists(Path.Combine(this.outputDirectory, "schema")), Is.False);
            Assert.That(File.ReadAllText(Path.Combine(this.outputDirectory, "metamodel", "elements", "WidgetBox.md")), Does.Contain("not serialized"));
        }

        [Test]
        public void Generate_is_byte_for_byte_deterministic_and_writes_lf_without_bom()
        {
            var first = Path.Combine(this.outputDirectory, "first");
            var second = Path.Combine(this.outputDirectory, "second");

            MetamodelGenerator.Generate(Fixture.Cmof, Fixture.Xsd, Fixture.Xsd, first);
            MetamodelGenerator.Generate(Fixture.Cmof, Fixture.Xsd, Fixture.Xsd, second);

            var firstFiles = Directory.GetFiles(first, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(first, f)).OrderBy(f => f, StringComparer.Ordinal).ToList();
            var secondFiles = Directory.GetFiles(second, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(second, f)).OrderBy(f => f, StringComparer.Ordinal).ToList();

            Assert.That(secondFiles, Is.EqualTo(firstFiles));
            foreach (var file in firstFiles)
            {
                var bytes = File.ReadAllBytes(Path.Combine(first, file));
                Assert.That(File.ReadAllBytes(Path.Combine(second, file)), Is.EqualTo(bytes), file);
                Assert.That(bytes.Take(3), Is.Not.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }), file);
                Assert.That(bytes.Contains((byte)'\r'), Is.False, file);
            }
        }
    }
}
