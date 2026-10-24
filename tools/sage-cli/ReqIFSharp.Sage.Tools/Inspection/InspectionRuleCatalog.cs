// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionRuleCatalog.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The table of every rule <see cref="ReqIFInspector"/> can report: the single source of each rule's severity,
    /// category and reference, so skills and documentation can list the rules from here.
    /// </summary>
    /// <remarks>
    /// Descriptions are short paraphrases written for this tool; they are not quotations of the ReqIF specification or
    /// of the prostep ivip Implementation Guide. Individual findings may cite a more specific clause than the rule's
    /// default <see cref="InspectionRule.Reference"/> (e.g. the clause of the element kind concerned).
    /// </remarks>
    public static class InspectionRuleCatalog
    {
        /// <summary>
        /// The prostep ivip ReqIF Implementation Guide version the G-xxx rules implement.
        /// </summary>
        public const string GuideVersion = "1.7";

        /// <summary>
        /// The prefix of every ReqIF 1.2 reference.
        /// </summary>
        internal const string Spec = "ReqIF 1.2 §";

        /// <summary>
        /// The prefix of every Implementation Guide reference.
        /// </summary>
        internal const string Guide = "PSI Implementation Guide " + GuideVersion + " §";

        private const string GuideCategory = "implementation-guide";

        private static readonly Dictionary<string, InspectionRule> RulesById;

        static InspectionRuleCatalog()
        {
            const string error = InspectionSeverity.Error;
            const string warning = InspectionSeverity.Warning;
            const string info = InspectionSeverity.Info;

            All =
            [
                new(RuleIds.Load, error, "load", null, "The file, archive or archive entry cannot be read as XML, is not a ReqIF document, or reqifsharp cannot deserialize it."),
                new(RuleIds.Xsd, error, "schema", Spec + "11", "The document does not validate against the ReqIF 1.2 XML schema (reqif.xsd with its XHTML driver) embedded in reqifsharp; XSD warnings are reported with severity warning."),
                new(RuleIds.Reader, warning, "reqifsharp", null, "reqifsharp logged a warning or error while reading the document, e.g. an element it does not support and skips."),
                new(RuleIds.Inspect, error, "inspection-failure", null, "A check could not be completed because of an unexpected problem in the inspector; the other findings are still valid."),

                new(RuleIds.DuplicateIdentifier, error, "duplicate-identifier", Spec + "10.8.32", "An IDENTIFIER is used by more than one element of the document; identifiers must be unique."),
                new(RuleIds.MissingIdentifier, error, "missing-identifier", Spec + "10.8.32", "An identifiable element has no IDENTIFIER, or an empty one."),
                new(RuleIds.MissingLastChange, error, "missing-last-change", Spec + "10.8.32", "An identifiable element has no LAST-CHANGE; reqifsharp then silently uses the time of reading."),
                new(RuleIds.UnresolvedReference, error, "unresolved-reference", Spec + "10.8", "A local *-REF does not resolve to any element of the document."),
                new(RuleIds.UnresolvedGlobalReference, warning, "unresolved-global-reference", Spec + "10.8.42", "A global reference (SpecRelation source/target, RelationGroup source/target specification) does not resolve in the document nor in any other document of the archive; it may point to another exchange document."),
                new(RuleIds.ReferenceKindMismatch, error, "reference-kind-mismatch", Spec + "10.8", "A *-REF resolves to an element of another kind than the reference names, e.g. a SPEC-OBJECT-TYPE-REF pointing to a SPECIFICATION-TYPE."),
                new(RuleIds.MissingReference, error, "missing-reference", Spec + "10.8", "A mandatory reference is absent or empty: the TYPE of a spec element, the DEFINITION of an attribute value, the TYPE of an attribute definition, the OBJECT of a SpecHierarchy, the SOURCE/TARGET of a SpecRelation or the source/target specification of a RelationGroup."),
                new(RuleIds.DefinitionKindMismatch, error, "definition-kind-mismatch", Spec + "10.5.2", "An attribute value refers to an attribute definition of another kind (e.g. ATTRIBUTE-VALUE-STRING to ATTRIBUTE-DEFINITION-INTEGER), or an attribute definition refers to a datatype of another kind."),
                new(RuleIds.AttributeNotInType, error, "attribute-not-in-type", Spec + "10.8.36", "An attribute value's definition is not one of the SPEC-ATTRIBUTES of the SpecType of the element that owns the value."),
                new(RuleIds.DuplicateAttributeValue, warning, "duplicate-attribute-value", Spec + "10.8.36", "An element holds more than one value for the same attribute definition."),
                new(RuleIds.IntegerOutOfRange, error, "value-out-of-range", Spec + "10.8.25", "An integer value lies outside the MIN..MAX of its DatatypeDefinitionInteger."),
                new(RuleIds.RealOutOfRange, error, "value-out-of-range", Spec + "10.8.26", "A real value lies outside the MIN..MAX of its DatatypeDefinitionReal."),
                new(RuleIds.RealAccuracy, warning, "real-accuracy", Spec + "10.8.26", "A real value has more decimal digits than the ACCURACY of its DatatypeDefinitionReal."),
                new(RuleIds.StringTooLong, error, "string-too-long", Spec + "10.8.28", "A string value is longer than the MAX-LENGTH of its DatatypeDefinitionString."),
                new(RuleIds.EnumValueNotInDatatype, error, "enum-value-not-in-datatype", Spec + "10.8.15", "An enumeration value refers to an ENUM-VALUE that is not one of the SPECIFIED-VALUES of the definition's DatatypeDefinitionEnumeration."),
                new(RuleIds.EnumNotMultiValued, error, "enum-not-multi-valued", Spec + "10.8.15", "An enumeration value holds more than one ENUM-VALUE-REF although its AttributeDefinitionEnumeration is not MULTI-VALUED."),
                new(RuleIds.DatatypeRangeInverted, warning, "datatype-range-inverted", Spec + "10.8.25", "A DatatypeDefinitionInteger or DatatypeDefinitionReal has a MIN greater than its MAX, so no value can be valid."),
                new(RuleIds.DefaultValueKindMismatch, error, "default-value-kind-mismatch", Spec + "10.8.3", "The DEFAULT-VALUE of an attribute definition is an attribute value of another kind than the definition."),
                new(RuleIds.DefaultValueDefinition, warning, "default-value-definition", Spec + "10.8.3", "The DEFAULT-VALUE of an attribute definition refers, through its own DEFINITION, to a different attribute definition."),
                new(RuleIds.MissingLongName, error, "missing-long-name", Spec + "10.8.3", "An AttributeDefinition, EnumValue or RelationGroup has no LONG-NAME, which the standard makes mandatory for these elements."),
                new(RuleIds.RelationGroupObjectOutsideSpecifications, warning, "relation-group-scope", Spec + "10.8.33", "A SpecRelation in a RelationGroup connects a SpecObject that is in neither the group's source nor its target specification."),
                new(RuleIds.DuplicateObjectInSpecification, info, "duplicate-object-in-specification", Spec + "10.8.37", "The same SpecObject is referenced by more than one SpecHierarchy of one specification tree."),
                new(RuleIds.OrphanSpecObject, info, "orphan", Spec + "10.4", "A SpecObject is not referenced from any SpecHierarchy, so it is not part of any specification."),
                new(RuleIds.HeaderReqIFVersion, warning, "header", Spec + "9.2.3", "The REQ-IF-HEADER has no REQ-IF-VERSION, or a value other than \"1.0\" (the value the schema fixes for ReqIF 1.0.1 to 1.2)."),
                new(RuleIds.HeaderToolId, warning, "header", Spec + "9.2.3", "The REQ-IF-HEADER has no, or an empty, REQ-IF-TOOL-ID or SOURCE-TOOL-ID, so the exporting tools cannot be identified."),
                new(RuleIds.HeaderField, info, "header", Spec + "9.2.3", "The REQ-IF-HEADER has no, or an empty, TITLE or CREATION-TIME."),
                new(RuleIds.XhtmlNamespace, error, "xhtml-namespace", Spec + "10.8.45", "The THE-VALUE or THE-ORIGINAL-VALUE of an XHTML value holds text or elements outside the XHTML namespace."),
                new(RuleIds.XhtmlElementNotAllowed, warning, "xhtml-element-not-allowed", Spec + "10.8.20", "An XHTML value uses an element outside the XHTML modules ReqIF allows (e.g. script, style, form, iframe, img, u, html, body)."),
                new(RuleIds.XhtmlClassAttribute, warning, "xhtml-class-attribute", Spec + "10.8.20", "An XHTML value uses the class attribute, which ReqIF forbids."),
                new(RuleIds.XhtmlObjectData, warning, "xhtml-object", Spec + "10.8.20", "An XHTML object has no data attribute, so the location of the external object is unknown."),
                new(RuleIds.XhtmlObjectAlternative, warning, "xhtml-object", Spec + "10.8.20", "An XHTML object that is not an image/png has no nested image/png alternative image with alternative text, or an alternative image is not image/png."),
                new(RuleIds.XhtmlObjectAttributes, warning, "xhtml-object", Spec + "10.8.20", "An XHTML object with a MIME type uses attributes other than data, type, width and height."),
                new(RuleIds.XhtmlObjectFileMissing, warning, "xhtml-object-file-missing", Spec + "10.8.20", "The relative data URL of an XHTML object points to a file that is neither in the .reqifz archive nor next to the .reqif file."),
                new(RuleIds.XhtmlSimplifiedWithoutOriginal, info, "xhtml-simplified", Spec + "10.8.20", "An XHTML value is marked IS-SIMPLIFIED but carries no THE-ORIGINAL-VALUE."),

                new(RuleIds.GuideVersion, info, GuideCategory, null, "The requested Implementation Guide version is not implemented; the rules of the implemented version were applied instead.", true),
                new(RuleIds.FileExtension, warning, GuideCategory, Guide + "2.1", "The file extension is not .reqif (single XML document) or .reqifz (ZIP archive), or does not match the content.", true),
                new(RuleIds.Encoding, warning, GuideCategory, Guide + "2.2", "The document has no XML declaration or is not encoded in UTF-8.", true),
                new(RuleIds.SystemAttributeUnknown, warning, GuideCategory, Guide + "2.3", "An attribute definition name starts with \"ReqIF.\" but is not one of the system attribute names of the guide; user-defined attributes should not use that prefix (see also §2.4.2).", true),
                new(RuleIds.SystemAttributeCase, warning, GuideCategory, Guide + "2.3", "An attribute definition name differs only in letter case from a system attribute name (e.g. \"Reqif.name\"); the names are case-sensitive on export.", true),
                new(RuleIds.SystemAttributeDatatype, warning, GuideCategory, Guide + "2.3", "A system attribute uses another kind of attribute definition than the guide recommends (e.g. ReqIF.Text should be XHTML, ReqIF.ForeignCreatedOn a date).", true),
                new(RuleIds.SystemAttributeLevel, info, GuideCategory, Guide + "2.3", "A system attribute is defined on a kind of SpecType (SpecObject, Specification or SpecRelation) for which the guide does not define it.", true),
                new(RuleIds.ForeignDeletedDefault, info, GuideCategory, Guide + "2.3", "ReqIF.ForeignDeleted has a default value other than false.", true),
                new(RuleIds.ObjectMimeType, warning, GuideCategory, Guide + "2.6.3", "An XHTML object has no type attribute; objects should always carry a MIME type (application/octet-stream when unknown).", true),
                new(RuleIds.ObjectAlternativeTextFormatted, info, GuideCategory, Guide + "2.6.3", "The alternative text of an embedded object is formatted XHTML; plain text is recommended.", true),
                new(RuleIds.XhtmlHeadings, info, GuideCategory, Guide + "2.7", "An XHTML value uses h1..h6 headings; headlines should rather be held in ReqIF.ChapterName with the level given by the SpecHierarchy nesting.", true),
                new(RuleIds.XhtmlMarginIndentation, info, GuideCategory, Guide + "2.7", "An XHTML value indents with a margin-left style, which importing tools need not support; blockquote is the recommended form.", true),
                new(RuleIds.ConversationIdentifier, warning, GuideCategory, Guide + "2.8", "The documents of one .reqifz archive carry different exchange conversation identifiers.", true),
                new(RuleIds.RelationWithoutGroup, warning, GuideCategory, Guide + "2.10", "A SpecRelation connects SpecObjects of different specifications but is not part of any RelationGroup.", true),
                new(RuleIds.RelationGroupTypeAttributes, warning, GuideCategory, Guide + "2.11", "A RelationGroupType defines attributes, although a RelationGroup cannot hold attribute values.", true),
                new(RuleIds.EditableAttributesTypeChange, info, GuideCategory, Guide + "2.17", "A SpecHierarchy inherits its editable attributes from an ancestor whose SpecObject has a different SpecObjectType; the set should be given explicitly.", true),
                new(RuleIds.EnumerationEditableConflict, info, GuideCategory, Guide + "2.18", "Enumeration attribute definitions with different IS-EDITABLE values share one DatatypeDefinitionEnumeration.", true),
                new(RuleIds.SchemaLocation, info, GuideCategory, Guide + "2.19", "xsi:schemaLocation does not point to the absolute OMG URL of the ReqIF schema.", true),
            ];

            RulesById = All.ToDictionary(rule => rule.Id, StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets every rule, engine rules (LOAD, XSD, READER, INSPECT) first, then the ReqIF 1.2 rules (R-xxx), then the
        /// Implementation Guide rules (G-xxx).
        /// </summary>
        public static IReadOnlyList<InspectionRule> All { get; }

        /// <summary>
        /// Gets the rule with the given id.
        /// </summary>
        /// <param name="id">The rule id.</param>
        /// <returns>The rule.</returns>
        /// <exception cref="KeyNotFoundException">There is no rule with that id.</exception>
        public static InspectionRule Get(string id) => RulesById[id];
    }
}
