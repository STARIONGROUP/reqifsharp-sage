// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFXmlNamesTestFixture.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen.Tests
{
    using ReqIFSharp.Sage.MetamodelGen.Naming;

    [TestFixture]
    public class ReqIFXmlNamesTestFixture
    {
        [TestCase("SpecObject", "SPEC-OBJECT")]
        [TestCase("ReqIF", "REQ-IF")]
        [TestCase("ReqIFHeader", "REQ-IF-HEADER")]
        [TestCase("ReqIFToolExtension", "REQ-IF-TOOL-EXTENSION")]
        [TestCase("DatatypeDefinitionXHTML", "DATATYPE-DEFINITION-XHTML")]
        [TestCase("AttributeValueXHTML", "ATTRIBUTE-VALUE-XHTML")]
        [TestCase("AlternativeID", "ALTERNATIVE-ID")]
        [TestCase("XhtmlContent", "XHTML-CONTENT")]
        [TestCase("longName", "LONG-NAME")]
        [TestCase("reqIFVersion", "REQ-IF-VERSION")]
        [TestCase("isTableInternal", "IS-TABLE-INTERNAL")]
        [TestCase("identifier", "IDENTIFIER")]
        [TestCase("attributeValue_", "ATTRIBUTE-VALUE")]
        public void ToXmlName_converts_metamodel_names_to_reqif_xml_names(string name, string expected)
        {
            Assert.That(ReqIFXmlNames.ToXmlName(name), Is.EqualTo(expected));
        }
    }
}
