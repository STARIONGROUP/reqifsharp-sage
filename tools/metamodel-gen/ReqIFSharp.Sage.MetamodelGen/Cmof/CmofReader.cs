// -------------------------------------------------------------------------------------------------
// <copyright file="CmofReader.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen.Cmof
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Reads a CMOF 2.0 file into a <see cref="CmofModel"/>. The OMG ReqIF file is an XMI 2.1 document
    /// with a <c>cmof:Package</c> root whose <c>ownedMember</c>s are classes, associations and nested
    /// packages, followed by top-level <c>cmof:Tag</c>s.
    /// </summary>
    /// <remarks>
    /// A plain <see cref="XDocument"/> reader rather than uml4net.xmi: the ReqIF metamodel is an
    /// Enterprise Architect CMOF export, not UML XMI, and it is small. Elements and attributes are
    /// matched by local name so that the exact XMI and MOF namespace versions do not matter.
    /// </remarks>
    public static class CmofReader
    {
        private static readonly IReadOnlyDictionary<string, string> NoTags = new Dictionary<string, string>();

        /// <summary>
        /// Reads the CMOF file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path of the CMOF file.</param>
        /// <returns>The model.</returns>
        /// <exception cref="InvalidDataException">The file contains no <c>cmof:Package</c>.</exception>
        public static CmofModel Read(string path)
        {
            using var stream = File.OpenRead(path);
            return Read(stream);
        }

        /// <summary>
        /// Reads a CMOF document from <paramref name="stream"/>.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <returns>The model.</returns>
        /// <exception cref="InvalidDataException">The document contains no <c>cmof:Package</c>.</exception>
        public static CmofModel Read(Stream stream)
        {
            var document = XDocument.Load(stream);
            var root = document.Root?.Name.LocalName == "Package"
                ? document.Root
                : document.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "Package");

            if (root is null)
            {
                throw new InvalidDataException("The document contains no top-level cmof:Package.");
            }

            var comments = ReadComments(document);
            var tags = ReadTags(document);
            var typeNames = document.Descendants()
                .Where(e => e.Name.LocalName == "ownedMember")
                .Select(e => (Id: XmiId(e), Name: (string?)e.Attribute("name")))
                .Where(t => t.Id is not null && t.Name is not null)
                .GroupBy(t => t.Id!, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First().Name!, StringComparer.Ordinal);

            var rootName = (string?)root.Attribute("name") ?? "Model";
            var reader = new PackageReader(comments, tags, typeNames);
            reader.ReadPackage(root, rootName);

            return new CmofModel(rootName, reader.Classes, reader.PrimitiveTypes, reader.Associations);
        }

        private static Dictionary<string, string> ReadComments(XDocument document)
        {
            return document.Descendants()
                .Where(e => e.Name.LocalName == "ownedComment")
                .Select(e => (
                    Targets: ((string?)e.Attribute("annotatedElement") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries),
                    Body: (string?)e.Attribute("body") ?? e.Elements().FirstOrDefault(c => c.Name.LocalName == "body")?.Value))
                .Where(c => !string.IsNullOrWhiteSpace(c.Body))
                .SelectMany(c => c.Targets.Select(t => (Target: t, Body: Normalize(c.Body!))))
                .GroupBy(c => c.Target, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => string.Join("\n\n", g.Select(c => c.Body)), StringComparer.Ordinal);
        }

        private static Dictionary<string, IReadOnlyDictionary<string, string>> ReadTags(XDocument document)
        {
            return document.Descendants()
                .Where(e => e.Name.LocalName == "Tag")
                .Select(e => (Elements: (string?)e.Attribute("element"), Name: (string?)e.Attribute("name"), Value: (string?)e.Attribute("value") ?? string.Empty))
                .Where(t => t.Elements is not null && t.Name is not null)
                .SelectMany(t => t.Elements!.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(id => (Id: id, Name: t.Name!, t.Value)))
                .GroupBy(t => t.Id, StringComparer.Ordinal)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyDictionary<string, string>)g.GroupBy(t => t.Name, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Last().Value, StringComparer.Ordinal),
                    StringComparer.Ordinal);
        }

        private static string? XmiId(XElement element)
        {
            return XmiAttribute(element, "id");
        }

        private static string? XmiType(XElement element)
        {
            var value = XmiAttribute(element, "type");
            if (value is null)
            {
                return null;
            }

            var colon = value.IndexOf(':');
            return colon >= 0 ? value[(colon + 1)..] : value;
        }

        private static string? XmiAttribute(XElement element, string localName)
        {
            return element.Attributes()
                .FirstOrDefault(a => a.Name.LocalName == localName && a.Name.NamespaceName.Contains("XMI", StringComparison.OrdinalIgnoreCase))?.Value;
        }

        private static string? HrefFragment(string? href)
        {
            if (href is null)
            {
                return null;
            }

            var hash = href.LastIndexOf('#');
            return hash >= 0 ? href[(hash + 1)..] : href;
        }

        private static string Normalize(string text)
        {
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        }

        private sealed class PackageReader
        {
            private readonly IReadOnlyDictionary<string, string> comments;

            private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> tags;

            private readonly IReadOnlyDictionary<string, string> typeNames;

            public PackageReader(
                IReadOnlyDictionary<string, string> comments,
                IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> tags,
                IReadOnlyDictionary<string, string> typeNames)
            {
                this.comments = comments;
                this.tags = tags;
                this.typeNames = typeNames;
            }

            public List<CmofClass> Classes { get; } = [];

            public List<CmofPrimitiveType> PrimitiveTypes { get; } = [];

            public List<CmofAssociation> Associations { get; } = [];

            public void ReadPackage(XElement package, string qualifiedName)
            {
                foreach (var member in package.Elements().Where(e => e.Name.LocalName == "ownedMember"))
                {
                    var id = XmiId(member) ?? string.Empty;
                    var name = (string?)member.Attribute("name");

                    switch (XmiType(member))
                    {
                        case "Package":
                            this.ReadPackage(member, $"{qualifiedName}::{name}");
                            break;
                        case "Class":
                            this.Classes.Add(this.ReadClass(member, id, name ?? id, qualifiedName));
                            break;
                        case "PrimitiveType":
                        case "DataType":
                            this.PrimitiveTypes.Add(new CmofPrimitiveType(id, name ?? id, qualifiedName, this.DocumentationOf(id)));
                            break;
                        case "Association":
                            var memberEnds = ((string?)member.Attribute("memberEnd") ?? string.Empty)
                                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                .ToList();
                            var ownedEnds = member.Elements()
                                .Where(e => e.Name.LocalName == "ownedEnd")
                                .Select(e => this.ReadProperty(e, id, true))
                                .ToList();
                            this.Associations.Add(new CmofAssociation(id, name, memberEnds, ownedEnds));
                            break;
                    }
                }
            }

            private CmofClass ReadClass(XElement member, string id, string name, string package)
            {
                var superClassIds = ((string?)member.Attribute("superClass") ?? string.Empty)
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Concat(member.Elements()
                        .Where(e => e.Name.LocalName == "superClass")
                        .Select(e => XmiAttribute(e, "idref") ?? HrefFragment((string?)e.Attribute("href")))
                        .OfType<string>())
                    .ToList();

                var attributes = member.Elements()
                    .Where(e => e.Name.LocalName == "ownedAttribute")
                    .Select(e => this.ReadProperty(e, id, false))
                    .ToList();

                return new CmofClass(
                    id,
                    name,
                    package,
                    (bool?)member.Attribute("isAbstract") ?? false,
                    superClassIds,
                    attributes,
                    this.DocumentationOf(id),
                    this.TagsOf(id));
            }

            private CmofProperty ReadProperty(XElement element, string ownerId, bool ownedByAssociation)
            {
                var id = XmiId(element) ?? string.Empty;
                var typeId = (string?)element.Attribute("type");
                string? typeName;

                if (typeId is not null)
                {
                    typeName = this.typeNames.TryGetValue(typeId, out var resolved) ? resolved : typeId;
                }
                else
                {
                    var typeElement = element.Elements().FirstOrDefault(e => e.Name.LocalName == "type");
                    var idref = typeElement is null ? null : XmiAttribute(typeElement, "idref");
                    if (idref is not null)
                    {
                        typeId = idref;
                        typeName = this.typeNames.TryGetValue(idref, out var resolved) ? resolved : idref;
                    }
                    else
                    {
                        typeName = HrefFragment((string?)typeElement?.Attribute("href"));
                    }
                }

                var lowerText = (string?)element.Attribute("lower");
                var upperText = (string?)element.Attribute("upper");

                var lower = int.TryParse(lowerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLower) ? parsedLower : 1;
                var upper = string.IsNullOrEmpty(upperText) ? "1" : upperText == "-1" ? "*" : upperText;

                return new CmofProperty(
                    id,
                    (string?)element.Attribute("name") ?? string.Empty,
                    ownerId,
                    typeId,
                    typeName,
                    lower,
                    upper,
                    (bool?)element.Attribute("isComposite") ?? false,
                    (bool?)element.Attribute("isOrdered") ?? false,
                    (string?)element.Attribute("association"),
                    ownedByAssociation,
                    this.DocumentationOf(id),
                    this.TagsOf(id));
            }

            private string? DocumentationOf(string id)
            {
                return this.comments.TryGetValue(id, out var body) ? body : null;
            }

            private IReadOnlyDictionary<string, string> TagsOf(string id)
            {
                return this.tags.TryGetValue(id, out var found) ? found : NoTags;
            }
        }
    }
}
