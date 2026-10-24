// -------------------------------------------------------------------------------------------------
// <copyright file="XmlDocumentationSet.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Documentation
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Linq;

    /// <summary>
    /// The <c>&lt;member&gt;</c> elements of one or more compiler-generated XML documentation files, keyed by
    /// documentation id (see <see cref="XmlDocIds"/>).
    /// </summary>
    public sealed class XmlDocumentationSet
    {
        private readonly Dictionary<string, XElement> members = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the number of documented members across all added files.
        /// </summary>
        public int Count => this.members.Count;

        /// <summary>
        /// Loads and adds the XML documentation file at <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path of the <c>.xml</c> documentation file.</param>
        /// <exception cref="FileNotFoundException">When the file does not exist.</exception>
        public void AddFile(string path)
        {
            using var stream = File.OpenRead(path);
            this.Add(XDocument.Load(stream, LoadOptions.PreserveWhitespace));
        }

        /// <summary>
        /// Adds the members of an already-parsed XML documentation document. When an id occurs more than once,
        /// the first occurrence wins.
        /// </summary>
        /// <param name="document">The <c>&lt;doc&gt;</c> document.</param>
        public void Add(XDocument document)
        {
            foreach (var member in document.Descendants("member"))
            {
                var name = (string?)member.Attribute("name");
                if (!string.IsNullOrEmpty(name))
                {
                    this.members.TryAdd(name, member);
                }
            }
        }

        /// <summary>
        /// Looks up the <c>&lt;member&gt;</c> element with documentation id <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The documentation id, e.g. <c>T:ReqIFSharp.SpecObject</c>.</param>
        /// <returns>The element, or <see langword="null"/> when the member is undocumented.</returns>
        public XElement? Find(string? id)
        {
            if (id is null)
            {
                return null;
            }

            return this.members.TryGetValue(id, out var member) ? member : null;
        }
    }
}
