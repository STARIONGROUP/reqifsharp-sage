// -------------------------------------------------------------------------------------------------
// <copyright file="XsdReaderTestFixture.cs" company="Starion Group S.A.">
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

    using ReqIFSharp.Sage.MetamodelGen.Xsd;

    [TestFixture]
    public class XsdReaderTestFixture
    {
        private XsdSchema schema = null!;

        [SetUp]
        public void SetUp()
        {
            this.schema = XsdReader.Read(Fixture.Xsd);
        }

        [Test]
        public void Read_returns_global_elements_complex_and_simple_types()
        {
            Assert.That(this.schema.TargetNamespace, Is.EqualTo("http://example.org/toy"));
            Assert.That(this.schema.GlobalElements.Single().Name, Is.EqualTo("WIDGET-BOX"));
            Assert.That(this.schema.GlobalElements.Single().Type, Is.EqualTo("WIDGET-BOX"));
            Assert.That(this.schema.ComplexTypes.Select(t => t.Name), Is.EqualTo(new[] { "GADGET", "WIDGET-BOX" }));
            Assert.That(this.schema.SimpleTypes.Single().Base, Is.EqualTo("xsd:IDREF"));
        }

        [Test]
        public void Read_reads_attributes_including_referenced_ones()
        {
            var widget = this.schema.ComplexTypes.Single(t => t.Name == "WIDGET-BOX");

            Assert.That(widget.Compositor, Is.EqualTo("all"));
            Assert.That(widget.Attributes.Select(a => a.Name), Is.EqualTo(new[] { "IDENTIFIER", "LONG-NAME", "xml:lang" }));
            Assert.That(widget.Attributes[0].Use, Is.EqualTo("required"));
            Assert.That(widget.Attributes[0].Type, Is.EqualTo("xsd:ID"));
            Assert.That(widget.Attributes[2].Type, Is.Null);
        }

        [Test]
        public void Read_reads_wrapper_elements_with_their_options_and_occurrences()
        {
            var widget = this.schema.ComplexTypes.Single(t => t.Name == "WIDGET-BOX");
            var gadgets = widget.Children.Single(c => c.Name == "GADGETS");
            var favourite = widget.Children.Single(c => c.Name == "FAVOURITE-GADGET");

            Assert.That(gadgets.MinOccurs, Is.EqualTo("0"));
            Assert.That(gadgets.OptionsMaxOccurs, Is.EqualTo("unbounded"));
            Assert.That(gadgets.Options.Single().Type, Is.EqualTo("GADGET"));
            Assert.That(gadgets.Options.Single().IsReference, Is.False);
            Assert.That(favourite.Options.Single().Name, Is.EqualTo("GADGET-REF"));
            Assert.That(favourite.Options.Single().IsReference, Is.True);
        }

        [Test]
        public void Read_reads_wildcards_inside_wrapper_elements()
        {
            var extras = this.schema.ComplexTypes.Single(t => t.Name == "WIDGET-BOX").Children.Single(c => c.Name == "EXTRAS");

            Assert.That(extras.Wildcard!.Namespace, Is.EqualTo("##other"));
            Assert.That(extras.Wildcard.ProcessContents, Is.EqualTo("lax"));
        }

        [Test]
        public void Read_reads_imports_and_model_groups()
        {
            Assert.That(this.schema.Imports.Single().Kind, Is.EqualTo("import"));
            var block = this.schema.Groups.Single(g => g.Name == "toy.Block.class");
            Assert.That(block.Elements, Is.EqualTo(new[] { "div" }));
            Assert.That(block.GroupReferences, Is.EqualTo(new[] { "toy.Inline.class" }));
        }

        [Test]
        public void Read_rejects_a_document_that_is_not_a_schema()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<root/>"));

            Assert.That(() => XsdReader.Read(stream), Throws.TypeOf<InvalidDataException>());
        }
    }
}
