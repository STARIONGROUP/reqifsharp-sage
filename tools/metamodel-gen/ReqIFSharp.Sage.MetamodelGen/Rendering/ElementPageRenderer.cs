// -------------------------------------------------------------------------------------------------
// <copyright file="ElementPageRenderer.cs" company="Starion Group S.A.">
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

    using ReqIFSharp.Sage.MetamodelGen.Cmof;
    using ReqIFSharp.Sage.MetamodelGen.Xsd;

    /// <summary>
    /// Renders the <c>metamodel/elements/&lt;Name&gt;.md</c> page of a metaclass or primitive type.
    /// </summary>
    public static class ElementPageRenderer
    {
        /// <summary>
        /// Renders the page of <paramref name="class"/>.
        /// </summary>
        /// <param name="view">The joined metamodel and schema.</param>
        /// <param name="class">The class to render.</param>
        /// <returns>The markdown page.</returns>
        public static string RenderClass(MetamodelView view, CmofClass @class)
        {
            var complexType = view.ComplexTypeOf(@class);
            var builder = new StringBuilder();

            builder.Append("---\n");
            builder.Append("name: ").Append(Markdown.Yaml(@class.Name)).Append('\n');
            builder.Append("kind: \"class\"\n");
            builder.Append("package: ").Append(Markdown.Yaml(@class.Package)).Append('\n');
            builder.Append("qualifiedName: ").Append(Markdown.Yaml(@class.QualifiedName)).Append('\n');
            builder.Append("isAbstract: ").Append(@class.IsAbstract ? "true" : "false").Append('\n');
            builder.Append("xmlElement: ").Append(Markdown.Yaml(complexType?.Name)).Append('\n');
            builder.Append("source: \"MODEL\"\n");
            builder.Append("---\n\n");

            builder.Append("# ").Append(@class.Name).Append(@class.IsAbstract ? " (abstract)" : string.Empty).Append("\n\n");

            builder.Append("## Description\n\n").Append(Markdown.Paragraphs(@class.Documentation)).Append("\n\n");

            builder.Append("## Generalizations\n\n").Append(Markdown.LinkList(view.DirectSuperClassesOf(@class).Select(c => c.Name))).Append("\n\n");
            builder.Append("## Specializations\n\n").Append(Markdown.LinkList(view.DirectSubclassesOf(@class).Select(c => c.Name))).Append("\n\n");

            var ancestors = view.AllAncestorsOf(@class);
            if (ancestors.Count > 0)
            {
                builder.Append("All ancestors (nearest first): ").Append(string.Join(", ", ancestors.Select(a => Markdown.Link(a.Name)))).Append("\n\n");
            }

            if (@class.IsAbstract)
            {
                var concrete = view.AllDescendantsOf(@class).Where(d => !d.IsAbstract).Select(d => Markdown.Link(d.Name)).ToList();
                builder.Append("Abstract: never written to a file itself. Concrete descendants: ")
                    .Append(concrete.Count == 0 ? "_none_" : string.Join(", ", concrete))
                    .Append(".\n\n");
            }

            builder.Append("## Owned features\n\n");
            AppendFeatureTable(builder, view, @class, @class.OwnedAttributes.OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => (Owner: @class, Feature: p)).ToList(), includeOwner: false);

            builder.Append("\n## Inherited features\n\n");
            AppendFeatureTable(builder, view, @class, view.InheritedFeaturesOf(@class), includeOwner: true);

            if (complexType is not null)
            {
                AppendSerialization(builder, view, @class, complexType);
            }

            var tags = @class.Tags.OrderBy(t => t.Key, StringComparer.Ordinal).ToList();
            if (tags.Count > 0)
            {
                builder.Append("\n## CMOF tags\n\n");
                foreach (var tag in tags)
                {
                    builder.Append("- `").Append(tag.Key).Append("` = `").Append(tag.Value).Append("`\n");
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Renders the page of <paramref name="primitiveType"/>.
        /// </summary>
        /// <param name="primitiveType">The primitive type to render.</param>
        /// <returns>The markdown page.</returns>
        public static string RenderPrimitiveType(CmofPrimitiveType primitiveType)
        {
            var builder = new StringBuilder();
            builder.Append("---\n");
            builder.Append("name: ").Append(Markdown.Yaml(primitiveType.Name)).Append('\n');
            builder.Append("kind: \"primitiveType\"\n");
            builder.Append("package: ").Append(Markdown.Yaml(primitiveType.Package)).Append('\n');
            builder.Append("qualifiedName: ").Append(Markdown.Yaml(primitiveType.QualifiedName)).Append('\n');
            builder.Append("source: \"MODEL\"\n");
            builder.Append("---\n\n");
            builder.Append("# ").Append(primitiveType.Name).Append("\n\n");
            builder.Append("Primitive type declared in `").Append(primitiveType.Package).Append("`.\n\n");
            builder.Append("## Description\n\n").Append(Markdown.Paragraphs(primitiveType.Documentation)).Append('\n');
            return builder.ToString();
        }

        private static void AppendFeatureTable(StringBuilder builder, MetamodelView view, CmofClass viewedFrom, IReadOnlyList<(CmofClass Owner, CmofProperty Feature)> features, bool includeOwner)
        {
            if (features.Count == 0)
            {
                builder.Append("_None._\n");
                return;
            }

            builder.Append(includeOwner ? "| Feature | Type | Multiplicity | Kind | Opposite | From | XML |\n|---|---|---|---|---|---|---|\n" : "| Feature | Type | Multiplicity | Kind | Opposite | XML |\n|---|---|---|---|---|---|\n");

            foreach (var (owner, feature) in features)
            {
                var opposite = view.Model.OppositeOf(feature);
                var typeIsClass = view.Model.FindClass(feature.TypeId) is not null;
                var typeCell = feature.TypeName is null ? string.Empty : typeIsClass ? Markdown.Link(feature.TypeName) : $"`{feature.TypeName}`";
                var kind = FeatureKind(feature, typeIsClass);

                builder.Append("| `").Append(feature.Name).Append("` | ")
                    .Append(typeCell).Append(" | `")
                    .Append(feature.Multiplicity).Append("` | ")
                    .Append(kind).Append(" | ")
                    .Append(opposite is null || string.IsNullOrEmpty(opposite.Name) ? string.Empty : $"`{opposite.Name}`" + (opposite.IsOwnedByAssociation ? " (not navigable)" : string.Empty)).Append(" | ");

                if (includeOwner)
                {
                    builder.Append(Markdown.Link(owner.Name)).Append(" | ");
                }

                builder.Append(view.BindingOf(viewedFrom, feature).Describe()).Append(" |\n");
            }
        }

        private static string FeatureKind(CmofProperty feature, bool typeIsClass)
        {
            var parts = new List<string>();
            parts.Add(!typeIsClass ? "value" : feature.IsComposite ? "composite" : "reference");
            if (feature.IsOrdered)
            {
                parts.Add("ordered");
            }

            return string.Join(", ", parts);
        }

        private static void AppendSerialization(StringBuilder builder, MetamodelView view, CmofClass @class, XsdComplexType complexType)
        {
            builder.Append("\n## XML serialization\n\n");
            builder.Append("Written as `<").Append(complexType.Name).Append(">` (schema complex type `").Append(complexType.Name)
                .Append("`, content model `xsd:").Append(complexType.Compositor).Append("`")
                .Append(complexType.Compositor == "all" ? ", so child elements may appear in any order" : complexType.Compositor == "sequence" ? ", so child elements must appear in this order" : string.Empty)
                .Append("). See also [schema/types/").Append(complexType.Name).Append(".md](../../schema/types/").Append(complexType.Name).Append(".md).\n\n");

            if (complexType.Attributes.Count > 0)
            {
                builder.Append("| XML attribute | Type | Use |\n|---|---|---|\n");
                foreach (var attribute in complexType.Attributes)
                {
                    builder.Append("| `").Append(attribute.Name).Append("` | ").Append(attribute.Type is null ? string.Empty : $"`{attribute.Type}`").Append(" | ").Append(attribute.Use).Append(" |\n");
                }

                builder.Append('\n');
            }

            if (complexType.Children.Count > 0)
            {
                builder.Append("| Child element | Occurs | Contains | Contents occur |\n|---|---|---|---|\n");
                foreach (var child in complexType.Children)
                {
                    var contains = child.Options.Count > 0
                        ? string.Join(", ", child.Options.Select(o => $"`<{o.Name}>`" + (o.IsReference ? " (reference by IDENTIFIER)" : string.Empty)))
                        : child.Type is null ? string.Empty : $"`{child.Type}` content";
                    if (child.Wildcard is not null)
                    {
                        contains += (contains.Length > 0 ? ", " : string.Empty) + $"any element from `{child.Wildcard.Namespace}`";
                    }

                    builder.Append("| `<").Append(child.Name).Append(">` | ").Append(child.MinOccurs).Append("..").Append(child.MaxOccurs).Append(" | ")
                        .Append(contains).Append(" | ")
                        .Append(child.OptionsMinOccurs is null ? string.Empty : $"{child.OptionsMinOccurs}..{child.OptionsMaxOccurs}").Append(" |\n");
                }

                builder.Append('\n');
            }

            var unbound = view.UnboundSchemaItemsOf(@class, complexType);
            if (unbound.Count > 0)
            {
                builder.Append("Schema items with no metamodel feature: ").Append(string.Join(", ", unbound.Select(u => $"`{u}`"))).Append(".\n\n");
            }

            builder.Append("Skeleton (DERIVED from the schema; required attributes only, every child shown once):\n\n```xml\n");
            builder.Append('<').Append(complexType.Name);
            foreach (var attribute in complexType.Attributes.Where(a => a.Use == "required"))
            {
                builder.Append(' ').Append(attribute.Name).Append("=\"...\"");
            }

            if (complexType.Children.Count == 0)
            {
                builder.Append("/>\n");
            }
            else
            {
                builder.Append(">\n");
                foreach (var child in complexType.Children)
                {
                    builder.Append("  <").Append(child.Name).Append('>');
                    var first = child.Options.FirstOrDefault();
                    if (first is not null)
                    {
                        builder.Append('<').Append(first.Name).Append('>').Append(first.IsReference ? "identifier" : "...").Append("</").Append(first.Name).Append('>');
                    }
                    else
                    {
                        builder.Append("...");
                    }

                    builder.Append("</").Append(child.Name).Append(">\n");
                }

                builder.Append("</").Append(complexType.Name).Append(">\n");
            }

            builder.Append("```\n");
        }
    }
}
