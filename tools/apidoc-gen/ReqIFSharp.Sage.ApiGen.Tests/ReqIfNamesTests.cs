// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIfNamesTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Tests
{
    using System.Linq;

    using ReqIFSharp.Sage.ApiGen;

    [TestFixture]
    public class ReqIfNamesTests
    {
        // Expected names verified against the element declarations of the OMG ReqIF 1.2 reqif.xsd.
        [TestCase("SpecObject", "SPEC-OBJECT")]
        [TestCase("SpecObjectType", "SPEC-OBJECT-TYPE")]
        [TestCase("SpecHierarchy", "SPEC-HIERARCHY")]
        [TestCase("SpecRelation", "SPEC-RELATION")]
        [TestCase("Specification", "SPECIFICATION")]
        [TestCase("RelationGroup", "RELATION-GROUP")]
        [TestCase("DatatypeDefinitionXHTML", "DATATYPE-DEFINITION-XHTML")]
        [TestCase("DatatypeDefinitionEnumeration", "DATATYPE-DEFINITION-ENUMERATION")]
        [TestCase("AttributeDefinitionXHTML", "ATTRIBUTE-DEFINITION-XHTML")]
        [TestCase("AttributeValueXHTML", "ATTRIBUTE-VALUE-XHTML")]
        [TestCase("AttributeValueBoolean", "ATTRIBUTE-VALUE-BOOLEAN")]
        [TestCase("ReqIF", "REQ-IF")]
        [TestCase("ReqIFHeader", "REQ-IF-HEADER")]
        [TestCase("ReqIFContent", "REQ-IF-CONTENT")]
        [TestCase("ReqIFToolExtension", "REQ-IF-TOOL-EXTENSION")]
        [TestCase("AlternativeID", "ALTERNATIVE-ID")]
        [TestCase("EnumValue", "ENUM-VALUE")]
        [TestCase("EmbeddedValue", "EMBEDDED-VALUE")]
        [TestCase("XHTMLContent", "XHTML-CONTENT")]
        public void ToXmlElementName_converts_pascal_case_to_upper_kebab_case(string name, string expected)
        {
            Assert.That(ReqIfNames.ToXmlElementName(name), Is.EqualTo(expected));
        }

        [TestCase("LongName", "longName")]
        [TestCase("IsTableInternal", "isTableInternal")]
        [TestCase("AlternativeID", "alternativeID")]
        [TestCase("ID", "id")]
        [TestCase("Type", "type")]
        public void ToFeatureName_converts_pascal_case_to_lower_camel_case(string name, string expected)
        {
            Assert.That(ReqIfNames.ToFeatureName(name), Is.EqualTo(expected));
        }

        [TestCase("Identifiable", "Description", "desc")]
        [TestCase("Identifiable", "AlternativeId", "alternativeID")]
        [TestCase("ReqIFContent", "DataTypes", "datatypes")]
        [TestCase("AttributeDefinitionEnumeration", "IsMultiValued", "multiValued")]
        [TestCase("Identifiable", "LongName", "longName")]
        public void FeatureName_applies_the_reqif_spelling_where_the_library_differs(string className, string propertyName, string expected)
        {
            Assert.That(ReqIfNames.FeatureName(className, propertyName), Is.EqualTo(expected));
        }

        [Test]
        public void MetaclassName_applies_the_reqif_spelling_where_the_library_differs()
        {
            Assert.Multiple(() =>
            {
                Assert.That(ReqIfNames.MetaclassName("AlternativeId"), Is.EqualTo("AlternativeID"));
                Assert.That(ReqIfNames.MetaclassName("SpecObject"), Is.EqualTo("SpecObject"));
            });
        }

        [Test]
        public void The_embedded_reqif_schema_declares_the_metaclass_elements()
        {
            var schema = ReqIfSchemaNames.FromAssembly(typeof(ReqIF).Assembly);

            Assert.That(schema, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(schema!.Contains("SPEC-OBJECT"), Is.True);
                Assert.That(schema.Contains("REQ-IF-HEADER"), Is.True);
                Assert.That(schema.Contains("EXTERNAL-OBJECT"), Is.False);
            });
        }

        [Test]
        public void Every_feature_of_a_reqif_metaclass_has_a_schema_xml_name_except_library_only_ones()
        {
            var libraryOnly = new[] { "ReqIF.Lang", "ReqIFToolExtension.InnerXml" };

            var missing = TestModel.Instance.Types
                .Where(type => type.ReqIfClass is not null)
                .SelectMany(type => type.Properties.Where(property => property.ReqIf is not null && property.ReqIf.XmlName is null).Select(property => type.Name + "." + property.Name))
                .ToList();

            Assert.That(missing, Is.EquivalentTo(libraryOnly));
        }

        [Test]
        public void A_decorated_class_without_a_schema_element_is_reported_as_library_specific()
        {
            var externalObject = TestModel.Type("ReqIFSharp.ExternalObject");
            var alternativeId = TestModel.Type("ReqIFSharp.AlternativeId");

            Assert.Multiple(() =>
            {
                Assert.That(externalObject.ReqIfClass, Is.Null);
                Assert.That(externalObject.XmlElement, Is.Null);
                Assert.That(externalObject.ReqIfNote, Does.Contain("ReqIFSharp-specific"));
                Assert.That(alternativeId.ReqIfClass, Is.EqualTo("AlternativeID"));
                Assert.That(alternativeId.XmlElement, Is.EqualTo("ALTERNATIVE-ID"));
            });
        }

        [Test]
        public void Every_concrete_reqif_class_attribute_name_matches_the_derived_xml_element_name()
        {
            var model = TestModel.Instance;
            var concrete = model.Types.Where(type => type.XmlElement is not null).ToList();

            Assert.That(concrete, Is.Not.Empty);
            Assert.Multiple(() =>
            {
                foreach (var type in concrete)
                {
                    Assert.That(type.XmlElement, Is.EqualTo(ReqIfNames.ToXmlElementName(type.Name)), type.FullName);
                }

                Assert.That(model.Warnings.Where(warning => warning.Contains("differs", System.StringComparison.Ordinal)), Is.Empty);
            });
        }
    }
}
