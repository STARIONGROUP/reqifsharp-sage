// -------------------------------------------------------------------------------------------------
// <copyright file="GuideChecker.cs" company="Starion Group S.A.">
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
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    /// <summary>
    /// The prostep ivip ReqIF Implementation Guide 1.7 rules (G-001 to G-017) that are not XHTML-related; the XHTML
    /// ones (G-008 to G-011) are in <see cref="XhtmlChecker"/>.
    /// </summary>
    internal static partial class GuideChecker
    {
        private const string SystemAttributePrefix = "ReqIF.";

        /// <summary>
        /// Checks the file extension against the content (G-001).
        /// </summary>
        /// <param name="path">The inspected path.</param>
        /// <param name="isArchive">Whether the content is a ZIP archive.</param>
        /// <param name="context">The context to report to.</param>
        internal static void CheckFileExtension(string path, bool isArchive, InspectionContext context)
        {
            var extension = Path.GetExtension(path);
            var expected = isArchive ? ".reqifz" : ".reqif";

            if (string.Equals(extension, expected, StringComparison.Ordinal))
            {
                return;
            }

            var message = string.Equals(extension, expected, StringComparison.OrdinalIgnoreCase)
                ? $"The file extension '{extension}' should be written '{expected}'."
                : $"The file has the extension '{(extension.Length == 0 ? "(none)" : extension)}' but holds {(isArchive ? "a ZIP archive" : "a single XML document")}; use '{expected}'{(isArchive ? " (not .zip)" : " (not .xml)")} so tools recognise it as ReqIF.";

            context.Report(RuleIds.FileExtension, message);
        }

        /// <summary>
        /// Checks the XML declaration and encoding of a document (G-002).
        /// </summary>
        /// <param name="source">The document.</param>
        /// <param name="context">The context to report to.</param>
        internal static void CheckEncoding(DocumentSource source, InspectionContext context)
        {
            var bytes = source.Content;

            if ((bytes.Length >= 2 && ((bytes[0] == 0xFE && bytes[1] == 0xFF) || (bytes[0] == 0xFF && bytes[1] == 0xFE)))
                || (bytes.Length >= 4 && bytes[0] == 0 && bytes[2] == 0))
            {
                context.Report(RuleIds.Encoding, "The document is encoded in UTF-16 (or UTF-32); ReqIF documents should be encoded in UTF-8.", line: 1);
                return;
            }

            var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
            var head = Encoding.ASCII.GetString(bytes, start, Math.Min(bytes.Length - start, 512));

            if (!head.StartsWith("<?xml", StringComparison.Ordinal))
            {
                context.Report(RuleIds.Encoding, "The document does not start with an XML declaration; it should start with <?xml version=\"1.0\" encoding=\"UTF-8\"?>.", line: 1);
                return;
            }

            var end = head.IndexOf("?>", StringComparison.Ordinal);
            var match = EncodingPattern().Match(end < 0 ? head : head[..end]);

            if (!match.Success)
            {
                context.Report(RuleIds.Encoding, "The XML declaration has no encoding; it should declare encoding=\"UTF-8\".", line: 1);
            }
            else if (!string.Equals(match.Groups[1].Value, "UTF-8", StringComparison.OrdinalIgnoreCase))
            {
                context.Report(RuleIds.Encoding, $"The XML declaration declares encoding=\"{match.Groups[1].Value}\"; ReqIF documents should be encoded in UTF-8.", line: 1);
            }
        }

        /// <summary>
        /// Checks that the documents of an archive carry the same exchange conversation identifier (G-012).
        /// </summary>
        /// <param name="documents">The documents of the archive.</param>
        /// <param name="context">The context to report to.</param>
        internal static void CheckConversationIdentifiers(IReadOnlyList<ReqIFXmlDocument> documents, InspectionContext context)
        {
            var identifiers = documents
                .Select(document => (document.Name, Identifiers: ConversationIdentifiers(document)))
                .Where(entry => entry.Identifiers.Count > 0)
                .ToList();

            var distinct = identifiers.SelectMany(entry => entry.Identifiers).Distinct(StringComparer.Ordinal).ToList();

            if (distinct.Count > 1)
            {
                var detail = string.Join("; ", identifiers.Select(entry => $"{entry.Name}: {string.Join(", ", entry.Identifiers)}"));
                context.Report(RuleIds.ConversationIdentifier, $"The documents of the archive carry {distinct.Count} different exchange conversation identifiers ({detail}); they should all be identical.");
            }
        }

        /// <summary>
        /// Runs the per-document guide rules.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <param name="context">The context to report to.</param>
        internal static void Run(ReqIFXmlDocument document, InspectionContext context)
        {
            CheckSystemAttributes(document, context);
            CheckRelationsWithoutGroup(document, context);
            CheckRelationGroupTypes(document, context);
            CheckEditableAttributeInheritance(document, context);
            CheckEnumerationEditableConflicts(document, context);
            CheckSchemaLocation(document, context);
        }

        private static List<string> ConversationIdentifiers(ReqIFXmlDocument document)
        {
            XNamespace prostep = ReqIFXml.ProstepNamespace;

            return document.Root.Descendants(prostep + "EXCHANGE-CONVERSATION")
                .SelectMany(conversation => conversation.Elements(prostep + "IDENTIFIER"))
                .Select(identifier => identifier.Value.Trim())
                .Where(identifier => identifier.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        private static void CheckSystemAttributes(ReqIFXmlDocument document, InspectionContext context)
        {
            foreach (var specType in document.SpecTypes)
            {
                var elementKind = specType.Name.LocalName switch
                {
                    "SPEC-OBJECT-TYPE" => SystemAttributeConventions.SpecObject,
                    "SPECIFICATION-TYPE" => SystemAttributeConventions.Specification,
                    "SPEC-RELATION-TYPE" => SystemAttributeConventions.SpecRelation,
                    _ => null,
                };

                foreach (var definition in ReqIFXmlDocument.AttributeDefinitionsOf(specType))
                {
                    var name = definition.Attribute("LONG-NAME")?.Value;

                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    var known = SystemAttributeConventions.FindName(name.Trim());

                    if (known is null)
                    {
                        if (name.StartsWith(SystemAttributePrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            var hint = name.Contains("Modified", StringComparison.OrdinalIgnoreCase) && name.Contains("On", StringComparison.Ordinal)
                                ? " The modification time belongs in the element's LAST-CHANGE instead."
                                : string.Empty;

                            context.Report(
                                RuleIds.SystemAttributeUnknown,
                                $"{ReqIFXml.Describe(definition)} in {ReqIFXml.Name(specType)} uses the ReqIF. prefix, but '{name}' is not one of the system attribute names of the guide; user-defined attributes should be named without that prefix.{hint}",
                                definition);
                        }

                        continue;
                    }

                    if (!string.Equals(known, name, StringComparison.Ordinal))
                    {
                        context.Report(
                            RuleIds.SystemAttributeCase,
                            $"{ReqIFXml.Describe(definition)} in {ReqIFXml.Name(specType)} is named '{name}'; the system attribute name is case-sensitive and should be written '{known}'.",
                            definition);
                    }

                    if (elementKind is null)
                    {
                        continue;
                    }

                    var convention = SystemAttributeConventions.Find(known, elementKind);
                    var definitionKind = ReqIFXml.Kind(definition.Name.LocalName, ReqIFXml.AttributeDefinitionPrefix);

                    if (convention is null)
                    {
                        var levels = SystemAttributeConventions.All.Where(candidate => candidate.Name == known).Select(candidate => candidate.ElementKind);

                        context.Report(
                            RuleIds.SystemAttributeLevel,
                            $"{ReqIFXml.Describe(definition)} defines the system attribute {known} on {ReqIFXml.Name(specType)}, but the guide defines {known} only for {string.Join(", ", levels)} elements.",
                            definition);
                    }
                    else if (definitionKind != convention.DefinitionKind)
                    {
                        context.Report(
                            RuleIds.SystemAttributeDatatype,
                            $"{ReqIFXml.Describe(definition)} is an {definition.Name.LocalName}; the guide recommends ATTRIBUTE-DEFINITION-{convention.DefinitionKind} for {known} on {elementKind} elements.",
                            definition);
                    }

                    if (known == "ReqIF.ForeignDeleted" && definitionKind == "BOOLEAN")
                    {
                        var defaultValue = ReqIFXml.Child(ReqIFXml.Child(definition, "DEFAULT-VALUE"), "ATTRIBUTE-VALUE-BOOLEAN");
                        var theValue = defaultValue is null ? null : ReqIFXml.Attribute(defaultValue, "THE-VALUE");

                        if (theValue is not null && theValue != "false" && theValue != "0")
                        {
                            context.Report(
                                RuleIds.ForeignDeletedDefault,
                                $"{ReqIFXml.Describe(definition)} has the default value {theValue}; ReqIF.ForeignDeleted should default to false, otherwise every object without an explicit value counts as deleted.",
                                defaultValue);
                        }
                    }
                }
            }
        }

        private static void CheckRelationsWithoutGroup(ReqIFXmlDocument document, InspectionContext context)
        {
            var grouped = document.RelationGroups
                .SelectMany(group => ReqIFXml.Children(ReqIFXml.Child(group, "SPEC-RELATIONS"), "SPEC-RELATION-REF"))
                .Select(reference => reference.Value.Trim())
                .ToHashSet(StringComparer.Ordinal);

            List<string> SpecificationsOf(string? objectId) =>
                objectId is null ? [] : document.ObjectIdsBySpecification.Where(pair => pair.Value.Contains(objectId)).Select(pair => pair.Key).ToList();

            foreach (var relation in document.SpecRelations)
            {
                var identifier = ReqIFXml.Attribute(relation, "IDENTIFIER");

                if (identifier is null || grouped.Contains(identifier))
                {
                    continue;
                }

                var sourceSpecifications = SpecificationsOf(ReqIFXml.ReferencedIdentifier(relation, "SOURCE"));
                var targetSpecifications = SpecificationsOf(ReqIFXml.ReferencedIdentifier(relation, "TARGET"));

                if (sourceSpecifications.Count > 0 && targetSpecifications.Count > 0 && !sourceSpecifications.Intersect(targetSpecifications).Any())
                {
                    context.Report(
                        RuleIds.RelationWithoutGroup,
                        $"{ReqIFXml.Describe(relation)} links specification '{sourceSpecifications[0]}' to specification '{targetSpecifications[0]}' but is not part of any RELATION-GROUP; the guide asks to group such cross-specification relations.",
                        relation);
                }
            }
        }

        private static void CheckRelationGroupTypes(ReqIFXmlDocument document, InspectionContext context)
        {
            foreach (var specType in document.SpecTypes.Where(type => type.Name.LocalName == "RELATION-GROUP-TYPE"))
            {
                var count = ReqIFXmlDocument.AttributeDefinitionsOf(specType).Count();

                if (count > 0)
                {
                    context.Report(
                        RuleIds.RelationGroupTypeAttributes,
                        $"{ReqIFXml.Describe(specType)} defines {count} attribute(s), but the schema gives RELATION-GROUP no VALUES, so they can never be filled; the guide recommends RelationGroupTypes without attributes.",
                        specType);
                }
            }
        }

        private static void CheckEditableAttributeInheritance(ReqIFXmlDocument document, InspectionContext context)
        {
            foreach (var hierarchy in document.SpecHierarchies)
            {
                if (ReqIFXml.Child(hierarchy, "EDITABLE-ATTS") is not null)
                {
                    continue;
                }

                var parent = hierarchy.Parent?.Parent;

                if (!ReqIFXml.Is(parent, "SPEC-HIERARCHY"))
                {
                    continue;
                }

                // Only relevant when an ancestor actually provides a set to inherit.
                var provider = parent!.AncestorsAndSelf().FirstOrDefault(ancestor => ReqIFXml.Is(ancestor, "SPEC-HIERARCHY") && ReqIFXml.Child(ancestor, "EDITABLE-ATTS") is not null);

                if (provider is null)
                {
                    continue;
                }

                var ownType = ReqIFXml.ReferencedIdentifier(document.ResolveReference(hierarchy, "OBJECT"), "TYPE");
                var parentType = ReqIFXml.ReferencedIdentifier(document.ResolveReference(parent, "OBJECT"), "TYPE");

                if (ownType is not null && parentType is not null && ownType != parentType)
                {
                    context.Report(
                        RuleIds.EditableAttributesTypeChange,
                        $"{ReqIFXml.Describe(hierarchy)} has no EDITABLE-ATTS and so inherits the set of SPEC-HIERARCHY '{ReqIFXml.Attribute(provider, "IDENTIFIER")}', but its object has SpecObjectType '{ownType}' while its parent's has '{parentType}'; give the set explicitly.",
                        hierarchy);
                }
            }
        }

        private static void CheckEnumerationEditableConflicts(ReqIFXmlDocument document, InspectionContext context)
        {
            var byDatatype = document.AttributeDefinitions
                .Where(definition => definition.Name.LocalName == "ATTRIBUTE-DEFINITION-ENUMERATION")
                .Select(definition => (Definition: definition, Datatype: ReqIFXml.ReferencedIdentifier(definition, "TYPE")))
                .Where(entry => entry.Datatype is not null)
                .GroupBy(entry => entry.Datatype!, StringComparer.Ordinal);

            foreach (var group in byDatatype)
            {
                var editable = group.Where(entry => IsEditable(entry.Definition)).Select(entry => entry.Definition).ToList();
                var readOnly = group.Where(entry => !IsEditable(entry.Definition)).Select(entry => entry.Definition).ToList();

                if (editable.Count > 0 && readOnly.Count > 0)
                {
                    var datatype = document.Resolve(group.Key);

                    context.Report(
                        RuleIds.EnumerationEditableConflict,
                        $"{(datatype is null ? $"DATATYPE-DEFINITION-ENUMERATION '{group.Key}'" : ReqIFXml.Describe(datatype))} is shared by editable ({string.Join(", ", editable.Select(ReqIFXml.Name))}) and non-editable ({string.Join(", ", readOnly.Select(ReqIFXml.Name))}) enumeration attributes; edits of its literals through the editable one also change the non-editable one. Use separate datatypes if that is not intended.",
                        datatype ?? editable[0]);
                }
            }

            static bool IsEditable(XElement definition) => ReqIFXml.Attribute(definition, "IS-EDITABLE") is "true" or "1";
        }

        private static void CheckSchemaLocation(ReqIFXmlDocument document, InspectionContext context)
        {
            var schemaLocation = document.Root.Attribute(XName.Get("schemaLocation", ReqIFXml.XsiNamespace))?.Value;

            if (string.IsNullOrWhiteSpace(schemaLocation))
            {
                return;
            }

            var tokens = schemaLocation.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            for (var index = 0; index + 1 < tokens.Length; index += 2)
            {
                if (tokens[index] != ReqIFXml.ReqIFNamespace)
                {
                    continue;
                }

                var location = tokens[index + 1];

                if (!Uri.TryCreate(location, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                {
                    context.Report(
                        RuleIds.SchemaLocation,
                        $"xsi:schemaLocation gives '{location}' as the location of the ReqIF schema; the guide recommends the absolute URL {ReqIFXml.ReqIFNamespace}.",
                        document.Root);
                }
            }
        }

        [GeneratedRegex("""encoding\s*=\s*["']([^"']+)["']""")]
        private static partial Regex EncodingPattern();
    }
}
