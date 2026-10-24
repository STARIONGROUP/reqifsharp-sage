// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIfSchemaNames.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    /// The XML element and attribute names declared by the ReqIF schema (<c>reqif.xsd</c>) that the ReqIFSharp
    /// assembly embeds as a resource for validation. Used to check derived element and feature names, and to
    /// tell ReqIF metaclasses apart from library-specific classes that carry the <c>ReqIfClass</c> attribute.
    /// The schema is read in-process from the referenced assembly, never copied.
    /// </summary>
    public sealed class ReqIfSchemaNames
    {
        private static readonly XNamespace Xsd = "http://www.w3.org/2001/XMLSchema";

        private readonly HashSet<string> names;

        private ReqIfSchemaNames(HashSet<string> names)
        {
            this.names = names;
        }

        /// <summary>
        /// Gets the number of distinct declared names.
        /// </summary>
        public int Count => this.names.Count;

        /// <summary>
        /// Loads the names from the <c>reqif.xsd</c> resource embedded in <paramref name="assembly"/>.
        /// </summary>
        /// <param name="assembly">The ReqIFSharp assembly.</param>
        /// <returns>The names, or <see langword="null"/> when the assembly embeds no <c>reqif.xsd</c>.</returns>
        public static ReqIfSchemaNames? FromAssembly(Assembly assembly)
        {
            var resource = assembly.GetManifestResourceNames()
                .Where(name => name.EndsWith(".reqif.xsd", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "reqif.xsd", StringComparison.OrdinalIgnoreCase))
                .OrderBy(name => name, StringComparer.Ordinal)
                .FirstOrDefault();

            if (resource is null)
            {
                return null;
            }

            using var stream = assembly.GetManifestResourceStream(resource);
            if (stream is null)
            {
                return null;
            }

            return FromSchema(XDocument.Load(stream));
        }

        /// <summary>
        /// Collects the <c>name</c> of every <c>xsd:element</c> and <c>xsd:attribute</c> declaration in a schema.
        /// </summary>
        /// <param name="schema">The XSD document.</param>
        /// <returns>The names.</returns>
        public static ReqIfSchemaNames FromSchema(XDocument schema)
        {
            var declared = schema.Descendants()
                .Where(element => element.Name == Xsd + "element" || element.Name == Xsd + "attribute")
                .Select(element => (string?)element.Attribute("name"))
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!);

            return new ReqIfSchemaNames(new HashSet<string>(declared, StringComparer.Ordinal));
        }

        /// <summary>
        /// Determines whether the schema declares an element or attribute named <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The XML name, e.g. <c>SPEC-OBJECT</c>.</param>
        /// <returns><see langword="true"/> when declared.</returns>
        public bool Contains(string? name)
        {
            return name is not null && this.names.Contains(name);
        }
    }
}
