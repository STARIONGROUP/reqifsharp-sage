// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFXml.cs" company="Starion Group S.A.">
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
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// Small helpers over the ReqIF XML vocabulary, shared by the checkers.
    /// </summary>
    internal static class ReqIFXml
    {
        /// <summary>
        /// The ReqIF XML namespace (the same for ReqIF 1.0.1, 1.1 and 1.2).
        /// </summary>
        internal const string ReqIFNamespace = "http://www.omg.org/spec/ReqIF/20110401/reqif.xsd";

        /// <summary>
        /// The XHTML namespace of THE-VALUE content.
        /// </summary>
        internal const string XhtmlNamespace = "http://www.w3.org/1999/xhtml";

        /// <summary>
        /// The prostep ivip tool extension namespace (conversation identifier, external links).
        /// </summary>
        internal const string ProstepNamespace = "http://www.prostep.org/reqif";

        /// <summary>
        /// The XML Schema instance namespace.
        /// </summary>
        internal const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";

        internal const string AttributeValuePrefix = "ATTRIBUTE-VALUE-";

        internal const string AttributeDefinitionPrefix = "ATTRIBUTE-DEFINITION-";

        internal const string DatatypePrefix = "DATATYPE-DEFINITION-";

        /// <summary>
        /// The clause of the ReqIF 1.2 specification that describes each element kind.
        /// </summary>
        private static readonly Dictionary<string, string> Clauses = new(StringComparer.Ordinal)
        {
            ["REQ-IF"] = "9.2.1",
            ["REQ-IF-HEADER"] = "9.2.3",
            ["ATTRIBUTE-DEFINITION-BOOLEAN"] = "10.8.4",
            ["ATTRIBUTE-DEFINITION-DATE"] = "10.8.5",
            ["ATTRIBUTE-DEFINITION-ENUMERATION"] = "10.8.6",
            ["ATTRIBUTE-DEFINITION-INTEGER"] = "10.8.7",
            ["ATTRIBUTE-DEFINITION-REAL"] = "10.8.8",
            ["ATTRIBUTE-DEFINITION-STRING"] = "10.8.10",
            ["ATTRIBUTE-DEFINITION-XHTML"] = "10.8.11",
            ["ATTRIBUTE-VALUE-BOOLEAN"] = "10.8.13",
            ["ATTRIBUTE-VALUE-DATE"] = "10.8.14",
            ["ATTRIBUTE-VALUE-ENUMERATION"] = "10.8.15",
            ["ATTRIBUTE-VALUE-INTEGER"] = "10.8.16",
            ["ATTRIBUTE-VALUE-REAL"] = "10.8.17",
            ["ATTRIBUTE-VALUE-STRING"] = "10.8.19",
            ["ATTRIBUTE-VALUE-XHTML"] = "10.8.20",
            ["DATATYPE-DEFINITION-BOOLEAN"] = "10.8.22",
            ["DATATYPE-DEFINITION-DATE"] = "10.8.23",
            ["DATATYPE-DEFINITION-ENUMERATION"] = "10.8.24",
            ["DATATYPE-DEFINITION-INTEGER"] = "10.8.25",
            ["DATATYPE-DEFINITION-REAL"] = "10.8.26",
            ["DATATYPE-DEFINITION-STRING"] = "10.8.28",
            ["DATATYPE-DEFINITION-XHTML"] = "10.8.29",
            ["ENUM-VALUE"] = "10.8.31",
            ["RELATION-GROUP"] = "10.8.33",
            ["RELATION-GROUP-TYPE"] = "10.8.34",
            ["SPEC-HIERARCHY"] = "10.8.37",
            ["SPECIFICATION"] = "10.8.38",
            ["SPECIFICATION-TYPE"] = "10.8.39",
            ["SPEC-OBJECT"] = "10.8.40",
            ["SPEC-OBJECT-TYPE"] = "10.8.41",
            ["SPEC-RELATION"] = "10.8.42",
            ["SPEC-RELATION-TYPE"] = "10.8.43",
        };

        /// <summary>
        /// Gets a value indicating whether <paramref name="element"/> is in the ReqIF namespace (or in no namespace,
        /// which reqifsharp accepts as well).
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns><c>true</c> for a ReqIF element.</returns>
        internal static bool IsReqIF(XElement element) => element.Name.NamespaceName == ReqIFNamespace || element.Name.NamespaceName.Length == 0;

        /// <summary>
        /// Gets a value indicating whether <paramref name="element"/> is the ReqIF element <paramref name="localName"/>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="localName">The ReqIF element name.</param>
        /// <returns><c>true</c> when it is.</returns>
        internal static bool Is(XElement? element, string localName) => element is not null && element.Name.LocalName == localName && IsReqIF(element);

        /// <summary>
        /// Gets the first ReqIF child element named <paramref name="localName"/>.
        /// </summary>
        /// <param name="element">The parent.</param>
        /// <param name="localName">The child name.</param>
        /// <returns>The child, or <c>null</c>.</returns>
        internal static XElement? Child(XElement? element, string localName) => element?.Elements().FirstOrDefault(child => Is(child, localName));

        /// <summary>
        /// Gets the ReqIF child elements named <paramref name="localName"/>.
        /// </summary>
        /// <param name="element">The parent.</param>
        /// <param name="localName">The child name.</param>
        /// <returns>The children.</returns>
        internal static IEnumerable<XElement> Children(XElement? element, string localName) =>
            element?.Elements().Where(child => Is(child, localName)) ?? [];

        /// <summary>
        /// Gets the trimmed value of an XML attribute, or <c>null</c> when it is absent.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="name">The attribute name.</param>
        /// <returns>The value.</returns>
        internal static string? Attribute(XElement element, string name) => element.Attribute(name)?.Value.Trim();

        /// <summary>
        /// Gets the 1-based line on which <paramref name="node"/> starts.
        /// </summary>
        /// <param name="node">The node.</param>
        /// <returns>The line, or <c>null</c> when line information is not available.</returns>
        internal static int? Line(XObject node) => node is IXmlLineInfo info && info.HasLineInfo() ? info.LineNumber : null;

        /// <summary>
        /// Gets the IDENTIFIER of <paramref name="element"/>, or of its nearest identifiable ancestor.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The identifier, or <c>null</c>.</returns>
        internal static string? OwnerIdentifier(XElement element)
        {
            for (var current = element; current is not null; current = current.Parent)
            {
                if (current.Name.LocalName == "ALTERNATIVE-ID")
                {
                    continue;
                }

                var identifier = Attribute(current, "IDENTIFIER");

                if (!string.IsNullOrEmpty(identifier))
                {
                    return identifier;
                }
            }

            return null;
        }

        /// <summary>
        /// Describes an element for a message, e.g. <c>SPEC-OBJECT '_abc' ("Brake")</c>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The description.</returns>
        internal static string Describe(XElement element)
        {
            var identifier = Attribute(element, "IDENTIFIER");
            var longName = element.Attribute("LONG-NAME")?.Value;
            var text = string.IsNullOrEmpty(identifier) ? $"{element.Name.LocalName} without IDENTIFIER" : $"{element.Name.LocalName} '{identifier}'";

            if (!string.IsNullOrEmpty(longName))
            {
                text += $" (\"{longName}\")";
            }

            var line = Line(element);

            return line is null ? text : $"{text} at line {line}";
        }

        /// <summary>
        /// Describes an element by name and identifier only, e.g. <c>SPEC-OBJECT '_abc'</c>.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <returns>The description.</returns>
        internal static string Name(XElement element)
        {
            var identifier = Attribute(element, "IDENTIFIER");
            var longName = element.Attribute("LONG-NAME")?.Value;
            var text = string.IsNullOrEmpty(identifier) ? element.Name.LocalName : $"{element.Name.LocalName} '{identifier}'";

            return string.IsNullOrEmpty(longName) ? text : $"{text} (\"{longName}\")";
        }

        /// <summary>
        /// Gets the first *-REF child of a reference container such as TYPE, DEFINITION or OBJECT.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <returns>The reference element, or <c>null</c>.</returns>
        internal static XElement? ReferenceElement(XElement? container) =>
            container?.Elements().FirstOrDefault(child => IsReqIF(child) && child.Name.LocalName.EndsWith("-REF", StringComparison.Ordinal));

        /// <summary>
        /// Gets the trimmed identifier the first *-REF child of <paramref name="owner"/>'s <paramref name="containerName"/> refers to.
        /// </summary>
        /// <param name="owner">The owning element, e.g. a SPEC-OBJECT.</param>
        /// <param name="containerName">The container, e.g. TYPE.</param>
        /// <returns>The identifier, or <c>null</c> when there is none or it is empty.</returns>
        internal static string? ReferencedIdentifier(XElement? owner, string containerName)
        {
            var value = ReferenceElement(Child(owner, containerName))?.Value.Trim();

            return string.IsNullOrEmpty(value) ? null : value;
        }

        /// <summary>
        /// Gets the kind suffix of an element name, e.g. "STRING" for ATTRIBUTE-VALUE-STRING with prefix ATTRIBUTE-VALUE-.
        /// </summary>
        /// <param name="localName">The element name.</param>
        /// <param name="prefix">The prefix.</param>
        /// <returns>The suffix, or <c>null</c> when the name does not start with the prefix.</returns>
        internal static string? Kind(string localName, string prefix)
        {
            if (!localName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            var kind = localName[prefix.Length..];

            return kind.EndsWith("-REF", StringComparison.Ordinal) ? kind[..^4] : kind;
        }

        /// <summary>
        /// Gets the ReqIF 1.2 reference of the clause describing an element kind.
        /// </summary>
        /// <param name="localName">The element name.</param>
        /// <returns>The reference, e.g. "ReqIF 1.2 §10.8.40", or <c>null</c> when the element kind has no own clause.</returns>
        internal static string? Clause(string localName) => Clauses.TryGetValue(localName, out var clause) ? InspectionRuleCatalog.Spec + clause : null;
    }
}
