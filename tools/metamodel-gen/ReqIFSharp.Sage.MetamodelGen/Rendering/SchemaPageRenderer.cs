// -------------------------------------------------------------------------------------------------
// <copyright file="SchemaPageRenderer.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.MetamodelGen.Rendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using ReqIFSharp.Sage.MetamodelGen.Xsd;

    /// <summary>
    /// Renders the <c>schema/types/&lt;NAME&gt;.md</c> pages of the ReqIF XML schema and the XHTML
    /// subset page derived from <c>driver.xsd</c>.
    /// </summary>
    public static class SchemaPageRenderer
    {
        /// <summary>
        /// Renders the page of a named complex type.
        /// </summary>
        /// <param name="view">The joined metamodel and schema.</param>
        /// <param name="complexType">The complex type.</param>
        /// <param name="schema">The schema it belongs to.</param>
        /// <returns>The markdown page.</returns>
        public static string RenderComplexType(MetamodelView view, XsdComplexType complexType, XsdSchema schema)
        {
            var @class = view.ClassOf(complexType);
            var globalElement = schema.GlobalElements.FirstOrDefault(e => e.Type == complexType.Name);
            var usedBy = UsagesOf(schema, complexType.Name);

            var builder = new StringBuilder();
            builder.Append("---\n");
            builder.Append("name: ").Append(Markdown.Yaml(complexType.Name)).Append('\n');
            builder.Append("kind: \"complexType\"\n");
            builder.Append("targetNamespace: ").Append(Markdown.Yaml(schema.TargetNamespace)).Append('\n');
            builder.Append("compositor: ").Append(Markdown.Yaml(complexType.Compositor)).Append('\n');
            builder.Append("metamodelClass: ").Append(Markdown.Yaml(@class?.Name)).Append('\n');
            builder.Append("globalElement: ").Append(Markdown.Yaml(globalElement?.Name)).Append('\n');
            builder.Append("source: \"MODEL\"\n");
            builder.Append("---\n\n");

            builder.Append("# ").Append(complexType.Name).Append("\n\n");

            if (@class is not null)
            {
                builder.Append("Serializes the metamodel class [").Append(@class.Name).Append("](../../metamodel/elements/").Append(@class.Name).Append(".md).\n\n");
            }

            if (globalElement is not null)
            {
                builder.Append("Declared as the global element `<").Append(globalElement.Name).Append(">` (the document root).\n\n");
            }

            builder.Append("Content model: `xsd:").Append(complexType.Compositor).Append("`.");
            if (complexType.GroupReference is not null)
            {
                builder.Append(" Content is the model group `").Append(complexType.GroupReference).Append("` (see [XHTML subset](../xhtml-subset.md)).");
            }

            builder.Append("\n\n");

            builder.Append("## Attributes\n\n");
            if (complexType.Attributes.Count == 0)
            {
                builder.Append("_None._\n\n");
            }
            else
            {
                builder.Append("| Attribute | Type | Use | Default | Fixed |\n|---|---|---|---|---|\n");
                foreach (var attribute in complexType.Attributes)
                {
                    builder.Append("| `").Append(attribute.Name).Append("` | ").Append(attribute.Type is null ? string.Empty : $"`{attribute.Type}`").Append(" | ")
                        .Append(attribute.Use).Append(" | ").Append(Markdown.Cell(attribute.Default)).Append(" | ").Append(Markdown.Cell(attribute.Fixed)).Append(" |\n");
                }

                builder.Append('\n');
            }

            builder.Append("## Child elements\n\n");
            if (complexType.Children.Count == 0 && complexType.Wildcard is null)
            {
                builder.Append("_None._\n\n");
            }
            else
            {
                builder.Append("| Element | Occurs | Type | Contains | Contents occur |\n|---|---|---|---|---|\n");
                foreach (var child in complexType.Children)
                {
                    var contains = string.Join(", ", child.Options.Select(o => $"`{o.Name}` : `{o.Type}`"));
                    if (child.Wildcard is not null)
                    {
                        contains += (contains.Length > 0 ? ", " : string.Empty) + Describe(child.Wildcard);
                    }

                    builder.Append("| `").Append(child.Name).Append("` | ").Append(child.MinOccurs).Append("..").Append(child.MaxOccurs).Append(" | ")
                        .Append(child.Type is null ? "(anonymous)" : TypeLink(schema, child.Type)).Append(" | ")
                        .Append(contains).Append(" | ")
                        .Append(child.OptionsMinOccurs is null ? string.Empty : $"{child.OptionsMinOccurs}..{child.OptionsMaxOccurs}").Append(" |\n");
                }

                if (complexType.Wildcard is not null)
                {
                    builder.Append("| (wildcard) | ").Append(complexType.Wildcard.MinOccurs).Append("..").Append(complexType.Wildcard.MaxOccurs)
                        .Append(" | | ").Append(Describe(complexType.Wildcard)).Append(" | |\n");
                }

                builder.Append('\n');
            }

            builder.Append("## Used by\n\n");
            builder.Append(usedBy.Count == 0 ? "_Not referenced by another complex type._" : string.Join("\n", usedBy.Select(u => $"- [{u.Owner}]({u.Owner}.md) as `{u.Path}`"))).Append('\n');

            return builder.ToString();
        }

        /// <summary>
        /// Renders the page of a named simple type.
        /// </summary>
        /// <param name="simpleType">The simple type.</param>
        /// <param name="schema">The schema it belongs to.</param>
        /// <returns>The markdown page.</returns>
        public static string RenderSimpleType(XsdSimpleType simpleType, XsdSchema schema)
        {
            var usedBy = UsagesOf(schema, simpleType.Name);
            var builder = new StringBuilder();
            builder.Append("---\n");
            builder.Append("name: ").Append(Markdown.Yaml(simpleType.Name)).Append('\n');
            builder.Append("kind: \"simpleType\"\n");
            builder.Append("base: ").Append(Markdown.Yaml(simpleType.Base)).Append('\n');
            builder.Append("source: \"MODEL\"\n");
            builder.Append("---\n\n");
            builder.Append("# ").Append(simpleType.Name).Append("\n\n");
            builder.Append("Restriction of `").Append(simpleType.Base ?? "?").Append("`.\n\n");

            if (simpleType.Name == "LOCAL-REF")
            {
                builder.Append("A reference to an element in the same document by its `IDENTIFIER` (an `xsd:IDREF`, so a validating parser checks that the target exists).\n\n");
            }
            else if (simpleType.Name == "GLOBAL-REF")
            {
                builder.Append("A reference by identifier that is not checked by the schema; it may point outside the document.\n\n");
            }

            builder.Append("## Used by\n\n");
            builder.Append(usedBy.Count == 0 ? "_Not referenced._" : string.Join("\n", usedBy.Select(u => $"- [{u.Owner}]({u.Owner}.md) as `{u.Path}`"))).Append('\n');
            return builder.ToString();
        }

        /// <summary>
        /// Renders <c>schema/xhtml-subset.md</c>: which XHTML modules <c>driver.xsd</c> pulls in and which
        /// elements each content group permits.
        /// </summary>
        /// <param name="driver">The parsed <c>driver.xsd</c>.</param>
        /// <returns>The markdown page.</returns>
        public static string RenderXhtmlSubset(XsdSchema driver)
        {
            var builder = new StringBuilder();
            builder.Append("---\n");
            builder.Append("name: \"XHTML subset\"\n");
            builder.Append("kind: \"xhtmlSubset\"\n");
            builder.Append("source: \"MODEL\"\n");
            builder.Append("---\n\n");
            builder.Append("# XHTML subset permitted in XHTML-CONTENT\n\n");
            builder.Append("`XHTML-CONTENT` (the type of `THE-VALUE` and `THE-ORIGINAL-VALUE` of `ATTRIBUTE-VALUE-XHTML`) is the model group `xhtml.BlkStruct.class` ")
                .Append("from the XHTML Modularization 1.1 driver schema that ReqIF publishes as `driver.xsd`. Content must be in the namespace `http://www.w3.org/1999/xhtml`.\n\n");

            builder.Append("## Modules included by driver.xsd\n\n");
            foreach (var import in driver.Imports)
            {
                var location = import.SchemaLocation ?? string.Empty;
                var file = location.Contains('/') ? location[(location.LastIndexOf('/') + 1)..] : location;
                builder.Append("- ").Append(import.Kind).Append(" `").Append(file).Append('`');
                if (import.Namespace is not null)
                {
                    builder.Append(" (namespace `").Append(import.Namespace).Append("`)");
                }

                builder.Append('\n');
            }

            builder.Append("\n## Content groups defined by driver.xsd\n\n");
            builder.Append("| Group | Elements | Includes groups |\n|---|---|---|\n");
            foreach (var group in driver.Groups)
            {
                builder.Append("| `").Append(group.Name).Append("` | ")
                    .Append(string.Join(", ", group.Elements.Distinct(StringComparer.Ordinal).Select(e => $"`{e}`"))).Append(" | ")
                    .Append(string.Join(", ", group.GroupReferences.Distinct(StringComparer.Ordinal).Select(g => $"`{g}`"))).Append(" |\n");
            }

            var reachable = ReachableElements(driver, "xhtml.BlkStruct.class");
            builder.Append("\n## Elements reachable directly from XHTML-CONTENT\n\n");
            builder.Append("DERIVED: the elements named in `xhtml.BlkStruct.class` and the groups it references within driver.xsd ")
                .Append("(groups defined in the module files themselves are not expanded here): ")
                .Append(reachable.Count == 0 ? "_none found_" : string.Join(", ", reachable.Select(e => $"`{e}`")))
                .Append(".\n");

            return builder.ToString();
        }

        private static IReadOnlyList<string> ReachableElements(XsdSchema driver, string groupName)
        {
            var groups = driver.Groups.ToDictionary(g => g.Name, StringComparer.Ordinal);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var elements = new SortedSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>();
            stack.Push(groupName);

            while (stack.Count > 0)
            {
                var name = stack.Pop();
                if (!seen.Add(name) || !groups.TryGetValue(name, out var group))
                {
                    continue;
                }

                elements.UnionWith(group.Elements);
                foreach (var reference in group.GroupReferences)
                {
                    stack.Push(reference);
                }
            }

            return elements.ToList();
        }

        private static string TypeLink(XsdSchema schema, string type)
        {
            return schema.ComplexTypes.Any(t => t.Name == type) || schema.SimpleTypes.Any(t => t.Name == type) ? $"[{type}]({type}.md)" : $"`{type}`";
        }

        private static string Describe(XsdWildcard wildcard)
        {
            return $"any element, namespace `{wildcard.Namespace}`, processContents `{wildcard.ProcessContents}`";
        }

        private static IReadOnlyList<(string Owner, string Path)> UsagesOf(XsdSchema schema, string typeName)
        {
            var result = new List<(string Owner, string Path)>();
            foreach (var owner in schema.ComplexTypes)
            {
                foreach (var attribute in owner.Attributes.Where(a => a.Type == typeName))
                {
                    result.Add((owner.Name, "@" + attribute.Name));
                }

                foreach (var child in owner.Children)
                {
                    if (child.Type == typeName)
                    {
                        result.Add((owner.Name, child.Name));
                    }

                    foreach (var option in child.Options.Where(o => o.Type == typeName))
                    {
                        result.Add((owner.Name, $"{child.Name}/{option.Name}"));
                    }
                }
            }

            return result
                .OrderBy(r => r.Owner, StringComparer.Ordinal)
                .ThenBy(r => r.Path, StringComparer.Ordinal)
                .ToList();
        }
    }
}
