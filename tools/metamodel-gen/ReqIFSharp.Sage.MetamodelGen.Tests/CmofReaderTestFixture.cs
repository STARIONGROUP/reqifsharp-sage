// -------------------------------------------------------------------------------------------------
// <copyright file="CmofReaderTestFixture.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen.Tests
{
    using System.IO;
    using System.Linq;
    using System.Text;

    using ReqIFSharp.Sage.MetamodelGen.Cmof;

    [TestFixture]
    public class CmofReaderTestFixture
    {
        private CmofModel model = null!;

        [SetUp]
        public void SetUp()
        {
            this.model = CmofReader.Read(Fixture.Cmof);
        }

        [Test]
        public void Read_returns_every_class_sorted_by_name_with_its_package()
        {
            Assert.That(this.model.Name, Is.EqualTo("Toy"));
            Assert.That(this.model.Classes.Select(c => c.Name), Is.EqualTo(new[] { "Gadget", "NamedThing", "WidgetBox" }));
            Assert.That(this.model.Classes.All(c => c.Package == "Toy"), Is.True);
        }

        [Test]
        public void Read_reports_abstract_classes_superclasses_and_documentation()
        {
            var named = this.model.Classes.Single(c => c.Name == "NamedThing");
            var widget = this.model.Classes.Single(c => c.Name == "WidgetBox");

            Assert.That(named.IsAbstract, Is.True);
            Assert.That(named.Documentation, Is.EqualTo("Anything with a name."));
            Assert.That(widget.IsAbstract, Is.False);
            Assert.That(widget.SuperClassIds, Is.EqualTo(new[] { "c_named" }));
            Assert.That(widget.Documentation, Is.EqualTo("A box of gadgets.\n\nSecond paragraph."));
        }

        [Test]
        public void Read_applies_cmof_multiplicity_defaults_and_resolves_types()
        {
            var named = this.model.Classes.Single(c => c.Name == "NamedThing");
            var identifier = named.OwnedAttributes.Single(p => p.Name == "identifier");
            var longName = named.OwnedAttributes.Single(p => p.Name == "longName");

            Assert.That(identifier.Multiplicity, Is.EqualTo("[1]"));
            Assert.That(identifier.TypeName, Is.EqualTo("ID"));
            Assert.That(identifier.TypeId, Is.EqualTo("t_id"));
            Assert.That(longName.Multiplicity, Is.EqualTo("[0..1]"));
            Assert.That(longName.TypeName, Is.EqualTo("String"));
            Assert.That(longName.TypeId, Is.Null);
        }

        [Test]
        public void Read_reads_composite_ordered_ends_and_their_tags()
        {
            var gadgets = this.model.Classes.Single(c => c.Name == "WidgetBox").OwnedAttributes.Single(p => p.Name == "gadgets");

            Assert.That(gadgets.IsComposite, Is.True);
            Assert.That(gadgets.IsOrdered, Is.True);
            Assert.That(gadgets.Multiplicity, Is.EqualTo("[0..*]"));
            Assert.That(gadgets.Tags["org.example.order"], Is.EqualTo("2"));
            Assert.That(this.model.Classes.Single(c => c.Name == "WidgetBox").Tags["org.example.global_element"], Is.EqualTo("True"));
        }

        [Test]
        public void Read_reads_primitive_types_from_nested_packages()
        {
            Assert.That(this.model.PrimitiveTypes.Single().QualifiedName, Is.EqualTo("Toy::Types::ID"));
        }

        [Test]
        public void OppositeOf_finds_class_owned_and_association_owned_ends()
        {
            var widget = this.model.Classes.Single(c => c.Name == "WidgetBox");

            var gadgetsOpposite = this.model.OppositeOf(widget.OwnedAttributes.Single(p => p.Name == "gadgets"));
            var favouriteOpposite = this.model.OppositeOf(widget.OwnedAttributes.Single(p => p.Name == "favouriteGadget"));

            Assert.That(gadgetsOpposite!.Name, Is.EqualTo("box"));
            Assert.That(gadgetsOpposite.IsOwnedByAssociation, Is.False);
            Assert.That(favouriteOpposite!.Name, Is.EqualTo("favouredBy"));
            Assert.That(favouriteOpposite.IsOwnedByAssociation, Is.True);
        }

        [Test]
        public void Read_rejects_a_document_without_a_package()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<xmi:XMI xmlns:xmi=\"http://schema.omg.org/spec/XMI/2.1\"/>"));

            Assert.That(() => CmofReader.Read(stream), Throws.TypeOf<InvalidDataException>());
        }
    }
}
