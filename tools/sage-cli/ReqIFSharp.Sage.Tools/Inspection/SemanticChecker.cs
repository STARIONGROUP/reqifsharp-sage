// -------------------------------------------------------------------------------------------------
// <copyright file="SemanticChecker.cs" company="Starion Group S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Xml.Linq;

    /// <summary>
    /// The ReqIF 1.2 rules R-001 to R-026 (identification, references, attribute values, specifications, header).
    /// The XHTML rules R-027 to R-034 are in <see cref="XhtmlChecker"/>.
    /// </summary>
    internal sealed class SemanticChecker
    {
        /// <summary>
        /// Reference containers whose referenced element is the target of a global (cross-document) reference.
        /// </summary>
        private static readonly HashSet<string> GlobalReferenceContainers = new(StringComparer.Ordinal) { "SOURCE", "TARGET", "SOURCE-SPECIFICATION", "TARGET-SPECIFICATION" };

        private readonly ReqIFXmlDocument document;

        private readonly InspectionContext context;

        private readonly IReadOnlySet<string> archiveIdentifiers;

        private readonly bool inArchive;

        /// <summary>
        /// Initializes a new instance of the <see cref="SemanticChecker"/> class.
        /// </summary>
        /// <param name="document">The document to check.</param>
        /// <param name="context">The context to report to.</param>
        /// <param name="archiveIdentifiers">Every identifier of every document of the file or archive, to resolve global references.</param>
        /// <param name="inArchive">Whether the document is an entry of a <c>.reqifz</c> archive.</param>
        internal SemanticChecker(ReqIFXmlDocument document, InspectionContext context, IReadOnlySet<string> archiveIdentifiers, bool inArchive)
        {
            this.inArchive = inArchive;
            this.document = document;
            this.context = context;
            this.archiveIdentifiers = archiveIdentifiers;
        }

        /// <summary>
        /// Runs every rule.
        /// </summary>
        internal void Run()
        {
            this.CheckHeader();
            this.CheckIdentification();
            this.CheckReferences();
            this.CheckMandatoryReferences();
            this.CheckDefinitionKinds();
            this.CheckAttributeMembership();
            this.CheckValues();
            this.CheckDatatypeRanges();
            this.CheckLongNames();
            this.CheckSpecificationTrees();
            this.CheckRelationGroups();
        }

        /// <summary>
        /// Gets the attribute definition that governs a value: its DEFINITION for an element value, the owning
        /// definition for a DEFAULT-VALUE. Only definitions of the value's own kind are returned.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="value">The ATTRIBUTE-VALUE-* element.</param>
        /// <returns>The definition, or <c>null</c>.</returns>
        internal static XElement? EffectiveDefinition(ReqIFXmlDocument document, XElement value)
        {
            var valueKind = ReqIFXml.Kind(value.Name.LocalName, ReqIFXml.AttributeValuePrefix);
            var definition = IsDefaultValue(value) ? value.Parent!.Parent : document.ResolveReference(value, "DEFINITION");

            return definition is not null && ReqIFXml.Kind(definition.Name.LocalName, ReqIFXml.AttributeDefinitionPrefix) == valueKind ? definition : null;
        }

        /// <summary>
        /// Gets the datatype of an attribute definition, when it resolves to a datatype of the definition's kind.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="definition">The ATTRIBUTE-DEFINITION-* element.</param>
        /// <returns>The datatype, or <c>null</c>.</returns>
        internal static XElement? DatatypeOf(ReqIFXmlDocument document, XElement? definition)
        {
            if (definition is null)
            {
                return null;
            }

            var datatype = document.ResolveReference(definition, "TYPE");
            var definitionKind = ReqIFXml.Kind(definition.Name.LocalName, ReqIFXml.AttributeDefinitionPrefix);

            return datatype is not null && ReqIFXml.Kind(datatype.Name.LocalName, ReqIFXml.DatatypePrefix) == definitionKind ? datatype : null;
        }

        /// <summary>
        /// Gets a value indicating whether an ATTRIBUTE-VALUE-* is the DEFAULT-VALUE of an attribute definition.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> for a default value.</returns>
        internal static bool IsDefaultValue(XElement value) => ReqIFXml.Is(value.Parent, "DEFAULT-VALUE") && value.Parent!.Parent is not null;

        private static string Located(XElement element)
        {
            var line = ReqIFXml.Line(element);
            return line is null ? string.Empty : $" (line {line})";
        }

        private static int FractionDigits(string lexical)
        {
            var text = lexical.Trim();
            var exponent = 0;
            var exponentIndex = text.IndexOfAny(['e', 'E']);

            if (exponentIndex >= 0)
            {
                int.TryParse(text[(exponentIndex + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent);
                text = text[..exponentIndex];
            }

            var dot = text.IndexOf('.');
            var fraction = dot < 0 ? string.Empty : text[(dot + 1)..].TrimEnd('0');

            return Math.Max(0, fraction.Length - exponent);
        }

        private void CheckHeader()
        {
            var header = this.document.Header;
            var where = header ?? this.document.Root;

            string? Field(string name)
            {
                var value = ReqIFXml.Child(header, name)?.Value.Trim();
                return string.IsNullOrEmpty(value) ? null : value;
            }

            var version = Field("REQ-IF-VERSION");

            if (version is null)
            {
                this.context.Report(RuleIds.HeaderReqIFVersion, "The REQ-IF-HEADER has no REQ-IF-VERSION; ReqIF 1.0.1 to 1.2 documents state \"1.0\".", where);
            }
            else if (version != "1.0")
            {
                this.context.Report(RuleIds.HeaderReqIFVersion, $"The REQ-IF-VERSION is \"{version}\"; the ReqIF schema fixes it to \"1.0\" (the value used by ReqIF 1.0.1 to 1.2 documents).", ReqIFXml.Child(header, "REQ-IF-VERSION"));
            }

            foreach (var name in new[] { "REQ-IF-TOOL-ID", "SOURCE-TOOL-ID" })
            {
                if (Field(name) is null)
                {
                    this.context.Report(RuleIds.HeaderToolId, $"The REQ-IF-HEADER has no {name} (or it is empty), so the {(name == "REQ-IF-TOOL-ID" ? "exporting ReqIF tool" : "source requirements authoring tool")} cannot be identified.", ReqIFXml.Child(header, name) ?? where);
                }
            }

            foreach (var name in new[] { "TITLE", "CREATION-TIME" })
            {
                if (Field(name) is null)
                {
                    this.context.Report(RuleIds.HeaderField, $"The REQ-IF-HEADER has no {name} (or it is empty).", ReqIFXml.Child(header, name) ?? where);
                }
            }

            if (header is not null && string.IsNullOrEmpty(ReqIFXml.Attribute(header, "IDENTIFIER")))
            {
                this.context.Report(RuleIds.MissingIdentifier, "The REQ-IF-HEADER has no IDENTIFIER (or it is empty).", header, reference: ReqIFXml.Clause("REQ-IF-HEADER"));
            }
        }

        private void CheckIdentification()
        {
            foreach (var element in this.document.Identifiables)
            {
                if (string.IsNullOrEmpty(ReqIFXml.Attribute(element, "IDENTIFIER")))
                {
                    this.context.Report(RuleIds.MissingIdentifier, $"{ReqIFXml.Describe(element)} has no IDENTIFIER (or it is empty); it cannot be referenced or matched on re-import.", element);
                }

                if (string.IsNullOrEmpty(ReqIFXml.Attribute(element, "LAST-CHANGE")))
                {
                    this.context.Report(RuleIds.MissingLastChange, $"{ReqIFXml.Describe(element)} has no LAST-CHANGE; reqifsharp then uses the time of reading, so the element looks changed on every import.", element);
                }
            }

            foreach (var (identifier, elements) in this.document.DuplicateIdentifiers)
            {
                var first = elements[0];

                foreach (var duplicate in elements.Skip(1))
                {
                    this.context.Report(
                        RuleIds.DuplicateIdentifier,
                        $"IDENTIFIER '{identifier}' of {ReqIFXml.Describe(duplicate)} is already used by {ReqIFXml.Describe(first)}; identifiers must be unique (reqifsharp throws when it looks such an element up).",
                        duplicate,
                        identifier: identifier);
                }
            }
        }

        private void CheckReferences()
        {
            foreach (var reference in this.document.References)
            {
                var identifier = reference.Value.Trim();
                var container = reference.Parent;

                if (identifier.Length == 0 || container is null)
                {
                    // An empty reference in a mandatory container is reported by CheckMandatoryReferences (R-007);
                    // anywhere else the schema already rejects it (IDREF).
                    continue;
                }

                var owner = container.Parent ?? container;
                var containerName = container.Name.LocalName;
                var target = this.document.Resolve(identifier);
                var path = $"{containerName}/{reference.Name.LocalName}";

                if (target is null)
                {
                    if (GlobalReferenceContainers.Contains(containerName))
                    {
                        if (!this.archiveIdentifiers.Contains(identifier))
                        {
                            this.context.Report(
                                RuleIds.UnresolvedGlobalReference,
                                $"{ReqIFXml.Describe(owner)} refers via {path} to '{identifier}', which is not defined in this document{(this.inArchive ? " nor in any other document of the archive" : string.Empty)}. Global references may point to another exchange document; otherwise this is a dangling link (reqifsharp substitutes an empty placeholder).",
                                reference,
                                identifier: ReqIFXml.OwnerIdentifier(owner),
                                reference: ReqIFXml.Clause(owner.Name.LocalName));
                        }
                    }
                    else
                    {
                        this.context.Report(
                            RuleIds.UnresolvedReference,
                            $"{ReqIFXml.Describe(owner)} refers via {path} to '{identifier}', which is not defined in this document.",
                            reference,
                            identifier: ReqIFXml.OwnerIdentifier(owner),
                            reference: ReqIFXml.Clause(owner.Name.LocalName));
                    }

                    continue;
                }

                var ownerName = owner.Name.LocalName;
                var checkedByKindRule = (containerName == "DEFINITION" && ownerName.StartsWith(ReqIFXml.AttributeValuePrefix, StringComparison.Ordinal))
                                        || (containerName == "TYPE" && ownerName.StartsWith(ReqIFXml.AttributeDefinitionPrefix, StringComparison.Ordinal));

                var expected = reference.Name.LocalName[..^"-REF".Length];

                if (!checkedByKindRule && target.Name.LocalName != expected)
                {
                    this.context.Report(
                        RuleIds.ReferenceKindMismatch,
                        $"{ReqIFXml.Describe(owner)} refers via {path} to '{identifier}', which is {ReqIFXml.Describe(target)}, not a {expected}.",
                        reference,
                        identifier: ReqIFXml.OwnerIdentifier(owner),
                        reference: ReqIFXml.Clause(ownerName));
                }
            }
        }

        private void CheckMandatoryReferences()
        {
            var mandatory = new List<(IEnumerable<XElement> Owners, string Container, string What)>
            {
                (this.document.SpecObjects, "TYPE", "SPEC-OBJECT-TYPE-REF"),
                (this.document.Specifications, "TYPE", "SPECIFICATION-TYPE-REF"),
                (this.document.SpecRelations, "TYPE", "SPEC-RELATION-TYPE-REF"),
                (this.document.SpecRelations, "SOURCE", "SPEC-OBJECT-REF"),
                (this.document.SpecRelations, "TARGET", "SPEC-OBJECT-REF"),
                (this.document.RelationGroups, "TYPE", "RELATION-GROUP-TYPE-REF"),
                (this.document.RelationGroups, "SOURCE-SPECIFICATION", "SPECIFICATION-REF"),
                (this.document.RelationGroups, "TARGET-SPECIFICATION", "SPECIFICATION-REF"),
                (this.document.SpecHierarchies, "OBJECT", "SPEC-OBJECT-REF"),
                (this.document.AttributeDefinitions, "TYPE", "DATATYPE-DEFINITION-*-REF"),
                (this.document.AttributeValues.Where(value => !IsDefaultValue(value)), "DEFINITION", "ATTRIBUTE-DEFINITION-*-REF"),
            };

            foreach (var (owners, container, what) in mandatory)
            {
                foreach (var owner in owners)
                {
                    if (ReqIFXml.ReferencedIdentifier(owner, container) is null)
                    {
                        var consequence = container switch
                        {
                            "SOURCE" or "TARGET" => " reqifsharp then leaves the relation end empty.",
                            "OBJECT" => " reqifsharp then leaves SpecHierarchy.Object null.",
                            "DEFINITION" => " reqifsharp cannot read such a value.",
                            _ => string.Empty,
                        };

                        this.context.Report(
                            RuleIds.MissingReference,
                            $"{ReqIFXml.Describe(owner)} has no {container} ({what}), or it is empty.{consequence}",
                            ReqIFXml.Child(owner, container) ?? owner,
                            identifier: ReqIFXml.OwnerIdentifier(owner),
                            reference: ReqIFXml.Clause(owner.Name.LocalName));
                    }
                }
            }
        }

        private void CheckDefinitionKinds()
        {
            foreach (var value in this.document.AttributeValues)
            {
                var valueKind = ReqIFXml.Kind(value.Name.LocalName, ReqIFXml.AttributeValuePrefix);
                var referenceElement = ReqIFXml.ReferenceElement(ReqIFXml.Child(value, "DEFINITION"));
                var definition = this.document.Resolve(referenceElement?.Value.Trim());

                if (IsDefaultValue(value))
                {
                    var owningDefinition = value.Parent!.Parent!;
                    var owningKind = ReqIFXml.Kind(owningDefinition.Name.LocalName, ReqIFXml.AttributeDefinitionPrefix);

                    if (owningKind is not null && owningKind != valueKind)
                    {
                        this.context.Report(
                            RuleIds.DefaultValueKindMismatch,
                            $"The DEFAULT-VALUE of {ReqIFXml.Describe(owningDefinition)} is an {value.Name.LocalName}; it must be an ATTRIBUTE-VALUE-{owningKind}.",
                            value,
                            reference: ReqIFXml.Clause(owningDefinition.Name.LocalName));
                    }
                    else if (definition is not null && definition != owningDefinition)
                    {
                        this.context.Report(
                            RuleIds.DefaultValueDefinition,
                            $"The DEFAULT-VALUE of {ReqIFXml.Describe(owningDefinition)} names {ReqIFXml.Describe(definition)} as its DEFINITION instead of the attribute definition that owns it.",
                            value,
                            reference: ReqIFXml.Clause(owningDefinition.Name.LocalName));
                    }

                    continue;
                }

                if (definition is null || referenceElement is null)
                {
                    continue;
                }

                var owner = value.Parent?.Parent ?? value;
                var definitionKind = ReqIFXml.Kind(definition.Name.LocalName, ReqIFXml.AttributeDefinitionPrefix);

                if (definitionKind is null)
                {
                    this.context.Report(
                        RuleIds.ReferenceKindMismatch,
                        $"The {value.Name.LocalName} of {ReqIFXml.Describe(owner)} has as DEFINITION '{definition.Attribute("IDENTIFIER")?.Value}', which is {ReqIFXml.Describe(definition)}, not an attribute definition.",
                        value,
                        reference: ReqIFXml.Clause(value.Name.LocalName));
                }
                else if (definitionKind != valueKind)
                {
                    this.context.Report(
                        RuleIds.DefinitionKindMismatch,
                        $"The {value.Name.LocalName} of {ReqIFXml.Describe(owner)} refers to {ReqIFXml.Describe(definition)}; a {valueKind} value needs an ATTRIBUTE-DEFINITION-{valueKind} (reqifsharp throws on such a value).",
                        value,
                        reference: ReqIFXml.Clause(value.Name.LocalName));
                }
                else if (ReqIFXml.Kind(referenceElement.Name.LocalName, ReqIFXml.AttributeDefinitionPrefix) != definitionKind)
                {
                    this.context.Report(
                        RuleIds.ReferenceKindMismatch,
                        $"The {value.Name.LocalName} of {ReqIFXml.Describe(owner)} refers to {ReqIFXml.Describe(definition)} with a {referenceElement.Name.LocalName}.",
                        value,
                        reference: ReqIFXml.Clause(value.Name.LocalName));
                }
            }

            foreach (var definition in this.document.AttributeDefinitions)
            {
                var referenceElement = ReqIFXml.ReferenceElement(ReqIFXml.Child(definition, "TYPE"));
                var datatype = this.document.Resolve(referenceElement?.Value.Trim());
                var definitionKind = ReqIFXml.Kind(definition.Name.LocalName, ReqIFXml.AttributeDefinitionPrefix);

                if (datatype is null || referenceElement is null || definitionKind is null)
                {
                    continue;
                }

                var datatypeKind = ReqIFXml.Kind(datatype.Name.LocalName, ReqIFXml.DatatypePrefix);

                if (datatypeKind is null)
                {
                    this.context.Report(
                        RuleIds.ReferenceKindMismatch,
                        $"{ReqIFXml.Describe(definition)} has as TYPE {ReqIFXml.Describe(datatype)}, which is not a datatype definition.",
                        definition,
                        reference: ReqIFXml.Clause(definition.Name.LocalName));
                }
                else if (datatypeKind != definitionKind)
                {
                    this.context.Report(
                        RuleIds.DefinitionKindMismatch,
                        $"{ReqIFXml.Describe(definition)} has as TYPE {ReqIFXml.Describe(datatype)}; an ATTRIBUTE-DEFINITION-{definitionKind} needs a DATATYPE-DEFINITION-{definitionKind} (reqifsharp throws an InvalidCastException on it).",
                        definition,
                        reference: ReqIFXml.Clause(definition.Name.LocalName));
                }
                else if (ReqIFXml.Kind(referenceElement.Name.LocalName, ReqIFXml.DatatypePrefix) != datatypeKind)
                {
                    this.context.Report(
                        RuleIds.ReferenceKindMismatch,
                        $"{ReqIFXml.Describe(definition)} refers to {ReqIFXml.Describe(datatype)} with a {referenceElement.Name.LocalName}.",
                        definition,
                        reference: ReqIFXml.Clause(definition.Name.LocalName));
                }
            }
        }

        private void CheckAttributeMembership()
        {
            var owners = this.document.SpecObjects.Concat(this.document.Specifications).Concat(this.document.SpecRelations).Concat(this.document.RelationGroups);

            foreach (var owner in owners)
            {
                var specType = this.document.ResolveReference(owner, "TYPE");
                var attributeIds = specType is null || !specType.Name.LocalName.EndsWith("-TYPE", StringComparison.Ordinal)
                    ? null
                    : ReqIFXmlDocument.AttributeDefinitionsOf(specType).Select(definition => ReqIFXml.Attribute(definition, "IDENTIFIER")).OfType<string>().ToHashSet(StringComparer.Ordinal);

                var seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (var value in ReqIFXmlDocument.ValuesOf(owner))
                {
                    var definitionId = ReqIFXml.ReferencedIdentifier(value, "DEFINITION");
                    var definition = this.document.Resolve(definitionId);

                    if (definitionId is null || definition is null)
                    {
                        continue;
                    }

                    if (!seen.Add(definitionId))
                    {
                        this.context.Report(
                            RuleIds.DuplicateAttributeValue,
                            $"{ReqIFXml.Describe(owner)} holds more than one value for {ReqIFXml.Name(definition)}; only one is expected per attribute definition.",
                            value);
                    }

                    if (attributeIds is not null && !attributeIds.Contains(definitionId))
                    {
                        this.context.Report(
                            RuleIds.AttributeNotInType,
                            $"{ReqIFXml.Describe(owner)} holds a value for {ReqIFXml.Name(definition)}, which is not one of the SPEC-ATTRIBUTES of its type {ReqIFXml.Name(specType!)} (reqifsharp accepts it, as it looks definitions up in all spec types).",
                            value);
                    }
                }
            }
        }

        private void CheckValues()
        {
            foreach (var value in this.document.AttributeValues)
            {
                var definition = EffectiveDefinition(this.document, value);
                var datatype = DatatypeOf(this.document, definition);

                if (definition is null || datatype is null)
                {
                    continue;
                }

                var subject = IsDefaultValue(value)
                    ? $"The DEFAULT-VALUE of {ReqIFXml.Describe(definition)}"
                    : $"The value of {ReqIFXml.Name(definition)} on {ReqIFXml.Describe(value.Parent?.Parent ?? value)}";

                switch (ReqIFXml.Kind(value.Name.LocalName, ReqIFXml.AttributeValuePrefix))
                {
                    case "INTEGER":
                        this.CheckInteger(value, datatype, subject);
                        break;
                    case "REAL":
                        this.CheckReal(value, datatype, subject);
                        break;
                    case "STRING":
                        this.CheckString(value, datatype, subject);
                        break;
                    case "ENUMERATION":
                        this.CheckEnumeration(value, definition, datatype, subject);
                        break;
                }
            }
        }

        private void CheckInteger(XElement value, XElement datatype, string subject)
        {
            var text = ReqIFXml.Attribute(value, "THE-VALUE");

            if (!BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
            {
                return;
            }

            var hasMin = BigInteger.TryParse(ReqIFXml.Attribute(datatype, "MIN"), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var min);
            var hasMax = BigInteger.TryParse(ReqIFXml.Attribute(datatype, "MAX"), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var max);

            if ((hasMin && number < min) || (hasMax && number > max))
            {
                this.context.Report(
                    RuleIds.IntegerOutOfRange,
                    $"{subject} is {text}, outside the range {(hasMin ? min.ToString(CultureInfo.InvariantCulture) : "?")}..{(hasMax ? max.ToString(CultureInfo.InvariantCulture) : "?")} of {ReqIFXml.Name(datatype)}.",
                    value);
            }
        }

        private void CheckReal(XElement value, XElement datatype, string subject)
        {
            var text = ReqIFXml.Attribute(value, "THE-VALUE");

            if (text is null || !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return;
            }

            var hasMin = double.TryParse(ReqIFXml.Attribute(datatype, "MIN"), NumberStyles.Float, CultureInfo.InvariantCulture, out var min);
            var hasMax = double.TryParse(ReqIFXml.Attribute(datatype, "MAX"), NumberStyles.Float, CultureInfo.InvariantCulture, out var max);

            if ((hasMin && number < min) || (hasMax && number > max))
            {
                this.context.Report(
                    RuleIds.RealOutOfRange,
                    $"{subject} is {text}, outside the range {(hasMin ? ReqIFXml.Attribute(datatype, "MIN") : "?")}..{(hasMax ? ReqIFXml.Attribute(datatype, "MAX") : "?")} of {ReqIFXml.Name(datatype)}.",
                    value);
            }

            if (int.TryParse(ReqIFXml.Attribute(datatype, "ACCURACY"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var accuracy) && accuracy >= 0)
            {
                var digits = FractionDigits(text);

                if (digits > accuracy)
                {
                    this.context.Report(
                        RuleIds.RealAccuracy,
                        $"{subject} is {text} with {digits} decimal digits, more than the ACCURACY {accuracy} of {ReqIFXml.Name(datatype)}; importing tools may round it.",
                        value);
                }
            }
        }

        private void CheckString(XElement value, XElement datatype, string subject)
        {
            var text = value.Attribute("THE-VALUE")?.Value;

            if (text is null || !int.TryParse(ReqIFXml.Attribute(datatype, "MAX-LENGTH"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var maxLength))
            {
                return;
            }

            var length = text.EnumerateRunes().Count();

            if (length > maxLength)
            {
                this.context.Report(
                    RuleIds.StringTooLong,
                    $"{subject} has {length} characters, more than the MAX-LENGTH {maxLength} of {ReqIFXml.Name(datatype)}.",
                    value);
            }
        }

        private void CheckEnumeration(XElement value, XElement definition, XElement datatype, string subject)
        {
            var references = ReqIFXml.Children(ReqIFXml.Child(value, "VALUES"), "ENUM-VALUE-REF").ToList();
            var datatypeId = ReqIFXml.Attribute(datatype, "IDENTIFIER");

            foreach (var reference in references)
            {
                var enumValue = this.document.Resolve(reference.Value.Trim());

                if (enumValue is null || enumValue.Name.LocalName != "ENUM-VALUE")
                {
                    continue;
                }

                var owningDatatype = enumValue.Parent?.Parent;

                if (owningDatatype is null || ReqIFXml.Attribute(owningDatatype, "IDENTIFIER") != datatypeId)
                {
                    this.context.Report(
                        RuleIds.EnumValueNotInDatatype,
                        $"{subject} refers to {ReqIFXml.Name(enumValue)}, which is not one of the SPECIFIED-VALUES of {ReqIFXml.Name(datatype)}{(owningDatatype is null ? string.Empty : $" but of {ReqIFXml.Name(owningDatatype)}")}.",
                        reference,
                        identifier: ReqIFXml.OwnerIdentifier(value));
                }
            }

            var multiValued = string.Equals(ReqIFXml.Attribute(definition, "MULTI-VALUED"), "true", StringComparison.Ordinal)
                              || ReqIFXml.Attribute(definition, "MULTI-VALUED") == "1";

            if (references.Count > 1 && !multiValued)
            {
                this.context.Report(
                    RuleIds.EnumNotMultiValued,
                    $"{subject} holds {references.Count} enumeration values, but {ReqIFXml.Name(definition)} is not MULTI-VALUED, so at most one is allowed.",
                    value);
            }
        }

        private void CheckDatatypeRanges()
        {
            foreach (var datatype in this.document.Datatypes)
            {
                var kind = ReqIFXml.Kind(datatype.Name.LocalName, ReqIFXml.DatatypePrefix);
                var minText = ReqIFXml.Attribute(datatype, "MIN");
                var maxText = ReqIFXml.Attribute(datatype, "MAX");

                var inverted = kind switch
                {
                    "INTEGER" => BigInteger.TryParse(minText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var min)
                                 && BigInteger.TryParse(maxText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var max) && min > max,
                    "REAL" => double.TryParse(minText, NumberStyles.Float, CultureInfo.InvariantCulture, out var realMin)
                              && double.TryParse(maxText, NumberStyles.Float, CultureInfo.InvariantCulture, out var realMax) && realMin > realMax,
                    _ => false,
                };

                if (inverted)
                {
                    this.context.Report(
                        RuleIds.DatatypeRangeInverted,
                        $"{ReqIFXml.Describe(datatype)} has MIN {minText} greater than MAX {maxText}, so no value can be valid.",
                        datatype,
                        reference: ReqIFXml.Clause(datatype.Name.LocalName));
                }
            }
        }

        private void CheckLongNames()
        {
            foreach (var element in this.document.AttributeDefinitions.Concat(this.document.EnumValues).Concat(this.document.RelationGroups))
            {
                if (string.IsNullOrWhiteSpace(element.Attribute("LONG-NAME")?.Value))
                {
                    var reference = element.Name.LocalName switch
                    {
                        "ENUM-VALUE" => ReqIFXml.Clause("ENUM-VALUE"),
                        "RELATION-GROUP" => ReqIFXml.Clause("RELATION-GROUP"),
                        _ => InspectionRuleCatalog.Spec + "10.8.3",
                    };

                    this.context.Report(
                        RuleIds.MissingLongName,
                        $"{ReqIFXml.Describe(element)} has no LONG-NAME, which is mandatory for {(element.Name.LocalName.StartsWith(ReqIFXml.AttributeDefinitionPrefix, StringComparison.Ordinal) ? "attribute definitions" : element.Name.LocalName == "ENUM-VALUE" ? "enumeration literals" : "relation groups")}.",
                        element,
                        reference: reference);
                }
            }
        }

        private void CheckSpecificationTrees()
        {
            var referenced = new HashSet<string>(StringComparer.Ordinal);

            foreach (var hierarchy in this.document.SpecHierarchies)
            {
                var objectId = ReqIFXml.ReferencedIdentifier(hierarchy, "OBJECT");

                if (objectId is not null)
                {
                    referenced.Add(objectId);
                }
            }

            foreach (var specification in this.document.Specifications)
            {
                var firstOccurrence = new Dictionary<string, XElement>(StringComparer.Ordinal);

                foreach (var hierarchy in specification.Descendants().Where(element => ReqIFXml.Is(element, "SPEC-HIERARCHY")))
                {
                    var objectId = ReqIFXml.ReferencedIdentifier(hierarchy, "OBJECT");

                    if (objectId is null)
                    {
                        continue;
                    }

                    if (firstOccurrence.TryGetValue(objectId, out var first))
                    {
                        this.context.Report(
                            RuleIds.DuplicateObjectInSpecification,
                            $"SPEC-OBJECT '{objectId}' appears more than once in {ReqIFXml.Name(specification)}: {ReqIFXml.Describe(hierarchy)} references it again after SPEC-HIERARCHY '{ReqIFXml.Attribute(first, "IDENTIFIER")}'{Located(first)}.",
                            hierarchy);
                    }
                    else
                    {
                        firstOccurrence.Add(objectId, hierarchy);
                    }
                }
            }

            foreach (var specObject in this.document.SpecObjects)
            {
                var identifier = ReqIFXml.Attribute(specObject, "IDENTIFIER");

                if (!string.IsNullOrEmpty(identifier) && !referenced.Contains(identifier))
                {
                    this.context.Report(
                        RuleIds.OrphanSpecObject,
                        $"{ReqIFXml.Describe(specObject)} is not referenced from any SPEC-HIERARCHY, so it is not part of any specification of this document.",
                        specObject);
                }
            }
        }

        private void CheckRelationGroups()
        {
            foreach (var group in this.document.RelationGroups)
            {
                var sourceId = ReqIFXml.ReferencedIdentifier(group, "SOURCE-SPECIFICATION");
                var targetId = ReqIFXml.ReferencedIdentifier(group, "TARGET-SPECIFICATION");

                if (sourceId is null || targetId is null
                    || !this.document.ObjectIdsBySpecification.TryGetValue(sourceId, out var sourceObjects)
                    || !this.document.ObjectIdsBySpecification.TryGetValue(targetId, out var targetObjects))
                {
                    continue;
                }

                foreach (var reference in ReqIFXml.Children(ReqIFXml.Child(group, "SPEC-RELATIONS"), "SPEC-RELATION-REF"))
                {
                    var relation = this.document.Resolve(reference.Value.Trim());

                    if (relation is null || relation.Name.LocalName != "SPEC-RELATION")
                    {
                        continue;
                    }

                    foreach (var end in new[] { "SOURCE", "TARGET" })
                    {
                        var objectId = ReqIFXml.ReferencedIdentifier(relation, end);

                        if (objectId is not null && this.document.Resolve(objectId) is not null && !sourceObjects.Contains(objectId) && !targetObjects.Contains(objectId))
                        {
                            this.context.Report(
                                RuleIds.RelationGroupObjectOutsideSpecifications,
                                $"{ReqIFXml.Describe(group)} contains {ReqIFXml.Name(relation)}, whose {end.ToLowerInvariant()} SPEC-OBJECT '{objectId}' is in neither its source specification '{sourceId}' nor its target specification '{targetId}'.",
                                reference,
                                identifier: ReqIFXml.Attribute(group, "IDENTIFIER"));
                        }
                    }
                }
            }
        }
    }
}
