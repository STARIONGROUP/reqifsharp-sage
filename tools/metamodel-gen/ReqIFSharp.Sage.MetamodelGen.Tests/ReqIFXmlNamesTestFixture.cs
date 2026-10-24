// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFXmlNamesTestFixture.cs" company="Starion Group S.A.">
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
