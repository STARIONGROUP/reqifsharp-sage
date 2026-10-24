// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFXmlDocument.cs" company="Starion Group S.A.">
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
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// An index over the XML of one ReqIF document, built with line information, on which the semantic rules run.
    /// </summary>
    /// <remarks>
    /// The rules run on the XML rather than on the reqifsharp object graph because reqifsharp cannot represent most
    /// of the defects they look for: an unresolved or wrongly typed attribute definition reference makes it throw, a
    /// duplicate IDENTIFIER makes its look-ups throw, an unresolved SpecRelation source/target or RelationGroup
    /// specification is replaced by a placeholder object, and a missing LAST-CHANGE is replaced by the time of reading.
    /// </remarks>
    internal sealed class ReqIFXmlDocument
    {
        private readonly Dictionary<string, List<XElement>> elementsById = new(StringComparer.Ordinal);

        private Dictionary<string, HashSet<string>>? objectIdsBySpecification;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReqIFXmlDocument"/> class.
        /// </summary>
        /// <param name="source">The document source.</param>
        /// <param name="xml">The parsed XML, with line information.</param>
        internal ReqIFXmlDocument(DocumentSource source, XDocument xml)
        {
            this.Source = source;
            this.Root = xml.Root!;
            this.Header = ReqIFXml.Child(ReqIFXml.Child(this.Root, "THE-HEADER"), "REQ-IF-HEADER");
            this.Elements = Walk(this.Root).ToList();

            foreach (var element in this.Elements)
            {
                var parent = element.Parent;
                var parentName = parent is not null && ReqIFXml.IsReqIF(parent) ? parent.Name.LocalName : string.Empty;
                var name = element.Name.LocalName;

                switch (parentName)
                {
                    case "SPEC-OBJECTS" when name == "SPEC-OBJECT":
                        this.SpecObjects.Add(element);
                        break;
                    case "SPECIFICATIONS" when name == "SPECIFICATION":
                        this.Specifications.Add(element);
                        break;
                    case "SPEC-RELATIONS" when name == "SPEC-RELATION":
                        this.SpecRelations.Add(element);
                        break;
                    case "SPEC-RELATION-GROUPS" when name == "RELATION-GROUP":
                        this.RelationGroups.Add(element);
                        break;
                    case "SPEC-TYPES":
                        this.SpecTypes.Add(element);
                        break;
                    case "DATATYPES":
                        this.Datatypes.Add(element);
                        break;
                    case "SPEC-ATTRIBUTES":
                        this.AttributeDefinitions.Add(element);
                        break;
                    case "SPECIFIED-VALUES" when name == "ENUM-VALUE":
                        this.EnumValues.Add(element);
                        break;
                }

                if (name == "SPEC-HIERARCHY")
                {
                    this.SpecHierarchies.Add(element);
                }
                else if (name.StartsWith(ReqIFXml.AttributeValuePrefix, StringComparison.Ordinal))
                {
                    this.AttributeValues.Add(element);
                }
                else if (name.EndsWith("-REF", StringComparison.Ordinal))
                {
                    this.References.Add(element);
                }

                var identifier = element.Attribute("IDENTIFIER")?.Value.Trim();

                if (name != "ALTERNATIVE-ID" && !string.IsNullOrEmpty(identifier))
                {
                    if (!this.elementsById.TryGetValue(identifier, out var list))
                    {
                        list = [];
                        this.elementsById.Add(identifier, list);
                    }

                    list.Add(element);
                }
            }
        }

        /// <summary>
        /// Gets the document source.
        /// </summary>
        internal DocumentSource Source { get; }

        /// <summary>
        /// Gets the document name used in findings.
        /// </summary>
        internal string Name => this.Source.Name;

        /// <summary>
        /// Gets the REQ-IF root element.
        /// </summary>
        internal XElement Root { get; }

        /// <summary>
        /// Gets the REQ-IF-HEADER element, when present.
        /// </summary>
        internal XElement? Header { get; }

        /// <summary>
        /// Gets every ReqIF element, in document order, outside tool extensions and XHTML content.
        /// </summary>
        internal IReadOnlyList<XElement> Elements { get; }

        /// <summary>
        /// Gets the SPEC-OBJECT elements.
        /// </summary>
        internal List<XElement> SpecObjects { get; } = [];

        /// <summary>
        /// Gets the SPECIFICATION elements.
        /// </summary>
        internal List<XElement> Specifications { get; } = [];

        /// <summary>
        /// Gets the SPEC-RELATION elements.
        /// </summary>
        internal List<XElement> SpecRelations { get; } = [];

        /// <summary>
        /// Gets the RELATION-GROUP elements.
        /// </summary>
        internal List<XElement> RelationGroups { get; } = [];

        /// <summary>
        /// Gets the spec type elements (children of SPEC-TYPES).
        /// </summary>
        internal List<XElement> SpecTypes { get; } = [];

        /// <summary>
        /// Gets the datatype elements (children of DATATYPES).
        /// </summary>
        internal List<XElement> Datatypes { get; } = [];

        /// <summary>
        /// Gets the attribute definition elements (children of SPEC-ATTRIBUTES).
        /// </summary>
        internal List<XElement> AttributeDefinitions { get; } = [];

        /// <summary>
        /// Gets the ENUM-VALUE elements.
        /// </summary>
        internal List<XElement> EnumValues { get; } = [];

        /// <summary>
        /// Gets the SPEC-HIERARCHY elements.
        /// </summary>
        internal List<XElement> SpecHierarchies { get; } = [];

        /// <summary>
        /// Gets the ATTRIBUTE-VALUE-* elements, both element values and default values.
        /// </summary>
        internal List<XElement> AttributeValues { get; } = [];

        /// <summary>
        /// Gets the *-REF elements.
        /// </summary>
        internal List<XElement> References { get; } = [];

        /// <summary>
        /// Gets every identifiable element (all of which must carry IDENTIFIER and LAST-CHANGE).
        /// </summary>
        internal IEnumerable<XElement> Identifiables =>
            this.Datatypes.Concat(this.EnumValues).Concat(this.SpecTypes).Concat(this.AttributeDefinitions)
                .Concat(this.SpecObjects).Concat(this.SpecRelations).Concat(this.Specifications).Concat(this.SpecHierarchies)
                .Concat(this.RelationGroups);

        /// <summary>
        /// Gets the identifiers used by more than one element, with those elements in document order.
        /// </summary>
        internal IEnumerable<KeyValuePair<string, List<XElement>>> DuplicateIdentifiers => this.elementsById.Where(pair => pair.Value.Count > 1);

        /// <summary>
        /// Gets every identifier in the document.
        /// </summary>
        internal IEnumerable<string> Identifiers => this.elementsById.Keys;

        /// <summary>
        /// Gets, per SPECIFICATION identifier, the identifiers of the SpecObjects its SpecHierarchy tree references.
        /// </summary>
        internal Dictionary<string, HashSet<string>> ObjectIdsBySpecification => this.objectIdsBySpecification ??= this.CollectObjectIdsBySpecification();

        /// <summary>
        /// Resolves an identifier to the (first) element that carries it.
        /// </summary>
        /// <param name="identifier">The identifier.</param>
        /// <returns>The element, or <c>null</c>.</returns>
        internal XElement? Resolve(string? identifier) =>
            identifier is not null && this.elementsById.TryGetValue(identifier, out var list) ? list[0] : null;

        /// <summary>
        /// Resolves the reference held by <paramref name="owner"/>'s <paramref name="containerName"/>.
        /// </summary>
        /// <param name="owner">The owning element.</param>
        /// <param name="containerName">The reference container, e.g. TYPE.</param>
        /// <returns>The referenced element, or <c>null</c>.</returns>
        internal XElement? ResolveReference(XElement? owner, string containerName) => this.Resolve(ReqIFXml.ReferencedIdentifier(owner, containerName));

        /// <summary>
        /// Gets the attribute definitions of a spec type.
        /// </summary>
        /// <param name="specType">The spec type.</param>
        /// <returns>The attribute definitions.</returns>
        internal static IEnumerable<XElement> AttributeDefinitionsOf(XElement specType) =>
            ReqIFXml.Child(specType, "SPEC-ATTRIBUTES")?.Elements().Where(ReqIFXml.IsReqIF) ?? [];

        /// <summary>
        /// Gets the element values (not default values) of an element with attributes.
        /// </summary>
        /// <param name="owner">A SPEC-OBJECT, SPECIFICATION, SPEC-RELATION or RELATION-GROUP.</param>
        /// <returns>The values.</returns>
        internal static IEnumerable<XElement> ValuesOf(XElement owner) =>
            ReqIFXml.Child(owner, "VALUES")?.Elements()
                .Where(value => ReqIFXml.IsReqIF(value) && value.Name.LocalName.StartsWith(ReqIFXml.AttributeValuePrefix, StringComparison.Ordinal)) ?? [];

        /// <summary>
        /// Builds the summary of the document.
        /// </summary>
        /// <returns>The summary.</returns>
        internal InspectedDocument ToInspectedDocument()
        {
            string? HeaderField(string name)
            {
                var value = ReqIFXml.Child(this.Header, name)?.Value.Trim();
                return string.IsNullOrEmpty(value) ? null : value;
            }

            return new InspectedDocument(
                this.Header is null ? null : ReqIFXml.Attribute(this.Header, "IDENTIFIER"),
                HeaderField("TITLE"),
                HeaderField("REQ-IF-VERSION"),
                HeaderField("SOURCE-TOOL-ID"),
                HeaderField("REQ-IF-TOOL-ID"),
                this.SpecObjects.Count,
                this.Specifications.Count,
                this.SpecRelations.Count,
                this.Datatypes.Count,
                this.SpecTypes.Count,
                this.Name);
        }

        private static IEnumerable<XElement> Walk(XElement element)
        {
            yield return element;

            if (element.Name.LocalName is "THE-VALUE" or "THE-ORIGINAL-VALUE")
            {
                yield break;
            }

            foreach (var child in element.Elements())
            {
                if (!ReqIFXml.IsReqIF(child) || child.Name.LocalName == "TOOL-EXTENSIONS")
                {
                    continue;
                }

                foreach (var descendant in Walk(child))
                {
                    yield return descendant;
                }
            }
        }

        private Dictionary<string, HashSet<string>> CollectObjectIdsBySpecification()
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var specification in this.Specifications)
            {
                var identifier = ReqIFXml.Attribute(specification, "IDENTIFIER");

                if (string.IsNullOrEmpty(identifier) || result.ContainsKey(identifier))
                {
                    continue;
                }

                var objectIds = specification.Descendants()
                    .Where(element => ReqIFXml.Is(element, "SPEC-HIERARCHY"))
                    .Select(hierarchy => ReqIFXml.ReferencedIdentifier(hierarchy, "OBJECT"))
                    .OfType<string>()
                    .ToHashSet(StringComparer.Ordinal);

                result.Add(identifier, objectIds);
            }

            return result;
        }
    }
}
