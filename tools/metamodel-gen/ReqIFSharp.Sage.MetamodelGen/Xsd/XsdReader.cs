// -------------------------------------------------------------------------------------------------
// <copyright file="XsdReader.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.MetamodelGen.Xsd
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Reads the structure of a single XML schema document (no import resolution) into an
    /// <see cref="XsdSchema"/>.
    /// </summary>
    /// <remarks>
    /// The ReqIF schema is regular enough that a structural <see cref="XDocument"/> walk is more useful
    /// than a compiled <c>XmlSchemaSet</c>: it keeps the wrapper-element / choice shape that the XML
    /// actually has, and it needs no network access to resolve the XHTML module imports.
    /// </remarks>
    public static class XsdReader
    {
        private static readonly XNamespace Xs = "http://www.w3.org/2001/XMLSchema";

        /// <summary>
        /// Reads the schema at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The schema file path.</param>
        /// <returns>The schema structure.</returns>
        public static XsdSchema Read(string path)
        {
            using var stream = File.OpenRead(path);
            return Read(stream);
        }

        /// <summary>
        /// Reads a schema document from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The schema structure.</returns>
        /// <exception cref="InvalidDataException">The document is not an XML schema.</exception>
        public static XsdSchema Read(Stream stream)
        {
            var document = XDocument.Load(stream);
            var schema = document.Root;
            if (schema is null || schema.Name != Xs + "schema")
            {
                throw new InvalidDataException("The document is not an XML schema (no xsd:schema root).");
            }

            var globalElements = schema.Elements(Xs + "element")
                .Select(e => new XsdGlobalElement((string?)e.Attribute("name") ?? string.Empty, LocalName((string?)e.Attribute("type"))))
                .OrderBy(e => e.Name, StringComparer.Ordinal)
                .ToList();

            var complexTypes = schema.Elements(Xs + "complexType")
                .Where(e => e.Attribute("name") is not null)
                .Select(ReadComplexType)
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();

            var simpleTypes = schema.Elements(Xs + "simpleType")
                .Where(e => e.Attribute("name") is not null)
                .Select(e => new XsdSimpleType((string)e.Attribute("name")!, (string?)e.Element(Xs + "restriction")?.Attribute("base")))
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();

            var imports = schema.Elements()
                .Where(e => e.Name == Xs + "import" || e.Name == Xs + "include")
                .Select(e => new XsdImport(e.Name.LocalName, (string?)e.Attribute("namespace"), (string?)e.Attribute("schemaLocation")))
                .ToList();

            var groups = schema.Elements(Xs + "group")
                .Where(e => e.Attribute("name") is not null)
                .Select(e => new XsdGroup(
                    (string)e.Attribute("name")!,
                    e.Descendants(Xs + "element").Select(x => (string?)x.Attribute("name") ?? (string?)x.Attribute("ref")).OfType<string>().ToList(),
                    e.Descendants(Xs + "group").Select(x => (string?)x.Attribute("ref")).OfType<string>().ToList()))
                .OrderBy(g => g.Name, StringComparer.Ordinal)
                .ToList();

            return new XsdSchema((string?)schema.Attribute("targetNamespace"), globalElements, complexTypes, simpleTypes, imports, groups);
        }

        private static XsdComplexType ReadComplexType(XElement complexType)
        {
            var name = (string)complexType.Attribute("name")!;
            var compositorElement = complexType.Elements().FirstOrDefault(e => e.Name == Xs + "all" || e.Name == Xs + "sequence" || e.Name == Xs + "choice" || e.Name == Xs + "group");
            var compositor = compositorElement?.Name.LocalName ?? "empty";

            var attributes = complexType.Elements(Xs + "attribute").Select(ReadAttribute).ToList();

            var children = new List<XsdChildElement>();
            XsdWildcard? wildcard = null;
            string? groupReference = null;

            if (compositorElement is not null)
            {
                if (compositorElement.Name == Xs + "group")
                {
                    groupReference = (string?)compositorElement.Attribute("ref");
                }
                else
                {
                    children.AddRange(compositorElement.Elements(Xs + "element").Select(ReadChild));
                    var any = compositorElement.Element(Xs + "any");
                    if (any is not null)
                    {
                        wildcard = ReadWildcard(any);
                    }
                }
            }

            return new XsdComplexType(name, compositor, attributes, children, groupReference, wildcard);
        }

        private static XsdAttribute ReadAttribute(XElement attribute)
        {
            return new XsdAttribute(
                (string?)attribute.Attribute("name") ?? (string?)attribute.Attribute("ref") ?? string.Empty,
                LocalName((string?)attribute.Attribute("type")),
                (string?)attribute.Attribute("use") ?? "optional",
                (string?)attribute.Attribute("default"),
                (string?)attribute.Attribute("fixed"));
        }

        private static XsdChildElement ReadChild(XElement element)
        {
            var options = new List<XsdChoiceOption>();
            string? optionsMin = null;
            string? optionsMax = null;
            XsdWildcard? wildcard = null;

            var inner = element.Element(Xs + "complexType")?.Elements()
                .FirstOrDefault(e => e.Name == Xs + "all" || e.Name == Xs + "sequence" || e.Name == Xs + "choice");

            if (inner is not null)
            {
                optionsMin = (string?)inner.Attribute("minOccurs") ?? "1";
                optionsMax = (string?)inner.Attribute("maxOccurs") ?? "1";
                options.AddRange(inner.Elements(Xs + "element").Select(o => new XsdChoiceOption(
                    (string?)o.Attribute("name") ?? (string?)o.Attribute("ref") ?? string.Empty,
                    LocalName((string?)o.Attribute("type")))));

                var any = inner.Element(Xs + "any");
                if (any is not null)
                {
                    wildcard = ReadWildcard(any);
                }
            }

            return new XsdChildElement(
                (string?)element.Attribute("name") ?? (string?)element.Attribute("ref") ?? string.Empty,
                (string?)element.Attribute("minOccurs") ?? "1",
                (string?)element.Attribute("maxOccurs") ?? "1",
                LocalName((string?)element.Attribute("type")),
                options,
                optionsMin,
                optionsMax,
                wildcard);
        }

        private static XsdWildcard ReadWildcard(XElement any)
        {
            return new XsdWildcard(
                (string?)any.Attribute("namespace") ?? "##any",
                (string?)any.Attribute("processContents") ?? "strict",
                (string?)any.Attribute("minOccurs") ?? "1",
                (string?)any.Attribute("maxOccurs") ?? "1");
        }

        private static string? LocalName(string? qualified)
        {
            if (qualified is null)
            {
                return null;
            }

            var colon = qualified.IndexOf(':');
            return colon >= 0 && !qualified.StartsWith("xsd:", StringComparison.Ordinal) && !qualified.StartsWith("xs:", StringComparison.Ordinal)
                ? qualified[(colon + 1)..]
                : qualified;
        }
    }
}
