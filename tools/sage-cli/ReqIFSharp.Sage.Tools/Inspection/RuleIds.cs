// -------------------------------------------------------------------------------------------------
// <copyright file="RuleIds.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Inspection
{
    /// <summary>
    /// The ids of the rules in <see cref="InspectionRuleCatalog"/>, so the checkers do not repeat string literals.
    /// </summary>
    internal static class RuleIds
    {
        internal const string Load = "LOAD";
        internal const string Xsd = "XSD";
        internal const string Reader = "READER";
        internal const string Inspect = "INSPECT";

        internal const string DuplicateIdentifier = "R-001";
        internal const string MissingIdentifier = "R-002";
        internal const string MissingLastChange = "R-003";
        internal const string UnresolvedReference = "R-004";
        internal const string UnresolvedGlobalReference = "R-005";
        internal const string ReferenceKindMismatch = "R-006";
        internal const string MissingReference = "R-007";
        internal const string DefinitionKindMismatch = "R-008";
        internal const string AttributeNotInType = "R-009";
        internal const string DuplicateAttributeValue = "R-010";
        internal const string IntegerOutOfRange = "R-011";
        internal const string RealOutOfRange = "R-012";
        internal const string RealAccuracy = "R-013";
        internal const string StringTooLong = "R-014";
        internal const string EnumValueNotInDatatype = "R-015";
        internal const string EnumNotMultiValued = "R-016";
        internal const string DatatypeRangeInverted = "R-017";
        internal const string DefaultValueKindMismatch = "R-018";
        internal const string DefaultValueDefinition = "R-019";
        internal const string MissingLongName = "R-020";
        internal const string RelationGroupObjectOutsideSpecifications = "R-021";
        internal const string DuplicateObjectInSpecification = "R-022";
        internal const string OrphanSpecObject = "R-023";
        internal const string HeaderReqIFVersion = "R-024";
        internal const string HeaderToolId = "R-025";
        internal const string HeaderField = "R-026";
        internal const string XhtmlNamespace = "R-027";
        internal const string XhtmlElementNotAllowed = "R-028";
        internal const string XhtmlClassAttribute = "R-029";
        internal const string XhtmlObjectData = "R-030";
        internal const string XhtmlObjectAlternative = "R-031";
        internal const string XhtmlObjectAttributes = "R-032";
        internal const string XhtmlObjectFileMissing = "R-033";
        internal const string XhtmlSimplifiedWithoutOriginal = "R-034";

        internal const string GuideVersion = "G-000";
        internal const string FileExtension = "G-001";
        internal const string Encoding = "G-002";
        internal const string SystemAttributeUnknown = "G-003";
        internal const string SystemAttributeCase = "G-004";
        internal const string SystemAttributeDatatype = "G-005";
        internal const string SystemAttributeLevel = "G-006";
        internal const string ForeignDeletedDefault = "G-007";
        internal const string ObjectMimeType = "G-008";
        internal const string ObjectAlternativeTextFormatted = "G-009";
        internal const string XhtmlHeadings = "G-010";
        internal const string XhtmlMarginIndentation = "G-011";
        internal const string ConversationIdentifier = "G-012";
        internal const string RelationWithoutGroup = "G-013";
        internal const string RelationGroupTypeAttributes = "G-014";
        internal const string EditableAttributesTypeChange = "G-015";
        internal const string EnumerationEditableConflict = "G-016";
        internal const string SchemaLocation = "G-017";
    }
}
