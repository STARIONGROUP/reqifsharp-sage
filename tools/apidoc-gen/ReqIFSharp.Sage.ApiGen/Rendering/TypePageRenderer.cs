// -------------------------------------------------------------------------------------------------
// <copyright file="TypePageRenderer.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.ApiGen.Rendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;

    using ReqIFSharp.Sage.ApiGen.Documentation;
    using ReqIFSharp.Sage.ApiGen.Model;

    /// <summary>
    /// Renders one <see cref="ApiTypeModel"/> to its <c>types/&lt;FullName&gt;.md</c> page: YAML front matter,
    /// documentation, inheritance, ReqIF mapping, and tables/sections for constructors, properties, methods,
    /// events, fields or enumeration literals, applicable extension methods and inherited members.
    /// Output uses LF line endings and is fully determined by the model.
    /// </summary>
    public sealed class TypePageRenderer
    {
        private readonly ApiModel model;

        private readonly DocMarkdownRenderer docs;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypePageRenderer"/> class.
        /// </summary>
        /// <param name="model">The API model, used to link documented types.</param>
        public TypePageRenderer(ApiModel model)
        {
            this.model = model;
            this.docs = new DocMarkdownRenderer(this.LinkForCref);
        }

        /// <summary>
        /// Gets the page file name of a type model, e.g. <c>ReqIFSharp.SpecObject.md</c>.
        /// </summary>
        /// <param name="type">The type model.</param>
        /// <returns>The file name.</returns>
        public static string FileNameOf(ApiTypeModel type)
        {
            return type.FullName + ".md";
        }

        /// <summary>
        /// Renders the page of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type model.</param>
        /// <returns>The markdown page.</returns>
        public string Render(ApiTypeModel type)
        {
            var builder = new StringBuilder();
            this.AppendFrontMatter(builder, type);

            builder.Append("# ").Append(type.Name).Append("\n\n");
            builder.Append("```csharp\n").Append(type.Declaration).Append("\n```\n\n");

            var facts = new List<string>
            {
                "Namespace: " + MarkdownText.Code(type.Namespace),
                "Assembly: " + MarkdownText.Code(type.Package.Assembly.GetName().Name ?? type.Package.PackageId),
                "Package: " + MarkdownText.Code(type.Package.PackageId + " " + type.Package.Version),
            };
            builder.Append(string.Join(" · ", facts)).Append("\n\n");

            builder.Append(this.docs.Render(type.Doc.Summary) ?? "_No summary available._").Append("\n\n");

            AppendDocSection(builder, "Remarks", this.docs.Render(type.Doc.Remarks));
            AppendDocSection(builder, "Example", this.docs.Render(type.Doc.Example));

            this.AppendTypeParameters(builder, type);
            this.AppendInheritance(builder, type);
            this.AppendReqIfMapping(builder, type);

            if (type.Kind == "enum")
            {
                this.AppendEnumLiterals(builder, type);
            }
            else
            {
                this.AppendMethodSection(builder, "Constructors", type.Constructors);
                this.AppendProperties(builder, type);
                this.AppendMethodSection(builder, "Methods", type.Methods);
                this.AppendEvents(builder, type);
                this.AppendFields(builder, type);
            }

            this.AppendAvailableExtensions(builder, type);
            this.AppendInheritedMembers(builder, type);

            return builder.ToString().TrimEnd('\n') + "\n";
        }

        /// <summary>
        /// Renders a link to the page of <paramref name="type"/> when it is documented, else a code span of its C# name.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The markdown.</returns>
        public string TypeLink(Type type)
        {
            var documented = !type.IsGenericType && !type.IsArray && !type.IsByRef ? this.model.Find(type) : null;
            var code = MarkdownText.Code(CSharpTypeNames.Format(type));
            return documented is null ? code : "[" + code + "](" + FileNameOf(documented) + ")";
        }

        private string? LinkForCref(string cref)
        {
            if (cref.Length < 3 || cref[1] != ':')
            {
                return null;
            }

            var body = cref[2..];
            var parenthesis = body.IndexOf('(', StringComparison.Ordinal);
            if (parenthesis >= 0)
            {
                body = body[..parenthesis];
            }

            if (cref[0] != 'T')
            {
                var dot = body.LastIndexOf('.');
                body = dot < 0 ? body : body[..dot];
            }

            foreach (var type in this.model.Types)
            {
                if (string.Equals(type.FullName, body, StringComparison.Ordinal))
                {
                    return FileNameOf(type);
                }
            }

            return null;
        }

        private void AppendFrontMatter(StringBuilder builder, ApiTypeModel type)
        {
            builder.Append("---\n");
            builder.Append("name: ").Append(MarkdownText.YamlString(type.Name)).Append('\n');
            builder.Append("fullName: ").Append(MarkdownText.YamlString(type.FullName)).Append('\n');
            builder.Append("namespace: ").Append(MarkdownText.YamlString(type.Namespace)).Append('\n');
            builder.Append("assembly: ").Append(MarkdownText.YamlString(type.Package.Assembly.GetName().Name)).Append('\n');
            builder.Append("package: ").Append(MarkdownText.YamlString(type.Package.PackageId)).Append('\n');
            builder.Append("packageVersion: ").Append(MarkdownText.YamlString(type.Package.Version)).Append('\n');
            builder.Append("kind: ").Append(MarkdownText.YamlString(type.Kind)).Append('\n');
            builder.Append("isAbstract: ").Append(MarkdownText.YamlBool(type.IsAbstract)).Append('\n');
            builder.Append("isStatic: ").Append(MarkdownText.YamlBool(type.IsStatic)).Append('\n');
            builder.Append("baseType: ").Append(MarkdownText.YamlString(type.BaseType is null ? null : CSharpTypeNames.Format(type.BaseType, qualified: true))).Append('\n');
            builder.Append("interfaces: ").Append(MarkdownText.YamlList(type.Interfaces.Select(i => CSharpTypeNames.Format(i, qualified: true)))).Append('\n');
            builder.Append("reqifClass: ").Append(MarkdownText.YamlString(type.ReqIfClass)).Append('\n');
            builder.Append("xmlElement: ").Append(MarkdownText.YamlString(type.XmlElement)).Append('\n');
            builder.Append("---\n\n");
        }

        private static void AppendDocSection(StringBuilder builder, string heading, string? text)
        {
            if (text is null)
            {
                return;
            }

            builder.Append("## ").Append(heading).Append("\n\n").Append(text).Append("\n\n");
        }

        private void AppendTypeParameters(StringBuilder builder, ApiTypeModel type)
        {
            if (!type.Type.IsGenericTypeDefinition)
            {
                return;
            }

            builder.Append("## Type parameters\n\n| Name | Description |\n|---|---|\n");
            foreach (var argument in type.Type.GetGenericArguments())
            {
                type.Doc.TypeParameters.TryGetValue(argument.Name, out var element);
                builder.Append("| ").Append(MarkdownText.CodeCell(argument.Name)).Append(" | ").Append(this.docs.RenderInline(element)).Append(" |\n");
            }

            builder.Append('\n');
        }

        private void AppendInheritance(StringBuilder builder, ApiTypeModel type)
        {
            if (type.Kind == "class")
            {
                builder.Append("## Inheritance\n\n");
                var steps = new List<string> { MarkdownText.Code("object") };
                steps.AddRange(type.InheritanceChain.Select(this.TypeLink));
                steps.Add("**" + MarkdownText.Code(CSharpTypeNames.Format(type.Type)) + "**");
                builder.Append(string.Join(" → ", steps)).Append("\n\n");
            }

            if (type.Interfaces.Count > 0)
            {
                builder.Append("## Implements\n\n");
                foreach (var @interface in type.Interfaces)
                {
                    builder.Append("- ").Append(this.InterfaceLink(@interface)).Append('\n');
                }

                builder.Append('\n');
            }

            if (type.DerivedTypes.Count > 0)
            {
                builder.Append(type.Kind == "interface" ? "## Implemented by\n\n" : "## Derived types\n\n");
                foreach (var derived in type.DerivedTypes)
                {
                    builder.Append("- ").Append(this.TypeLink(derived.Type)).Append('\n');
                }

                builder.Append('\n');
            }
        }

        private string InterfaceLink(Type @interface)
        {
            if (!@interface.IsGenericType)
            {
                return this.TypeLink(@interface);
            }

            return MarkdownText.Code(CSharpTypeNames.Format(@interface));
        }

        private void AppendReqIfMapping(StringBuilder builder, ApiTypeModel type)
        {
            var features = this.MappedFeatures(type).ToList();
            if (type.ReqIfClass is null && type.ReqIfNote is null && features.Count == 0)
            {
                return;
            }

            builder.Append("## ReqIF mapping\n\n");

            if (type.ReqIfNote is not null)
            {
                builder.Append(type.ReqIfNote).Append('\n');
            }

            if (type.ReqIfClass is not null)
            {
                builder.Append("- ReqIF metaclass: ").Append(MarkdownText.Code(type.ReqIfClass)).Append(type.IsAbstract ? " (abstract)" : string.Empty).Append('\n');
                builder.Append("- XML element: ")
                    .Append(type.XmlElement is null ? "_none - abstract metaclasses are serialized through their concrete subclasses_" : MarkdownText.Code("<" + type.XmlElement + ">"))
                    .Append('\n');
            }

            if (features.Count > 0)
            {
                builder.Append("\n| ReqIF feature | XML name | C# property | Type | Multiplicity | Aggregation | Declared by |\n|---|---|---|---|---|---|---|\n");
                foreach (var (owner, property) in features)
                {
                    var feature = property.ReqIf!;
                    builder.Append("| ").Append(MarkdownText.CodeCell(feature.Feature))
                        .Append(" | ").Append(feature.XmlName is null ? string.Empty : MarkdownText.CodeCell(feature.XmlName))
                        .Append(" | ").Append(MarkdownText.CodeCell(property.Name))
                        .Append(" | ").Append(MarkdownText.EscapeTableCell(this.TypeLink(property.Property.PropertyType)))
                        .Append(" | ").Append(MarkdownText.CodeCell(feature.Multiplicity))
                        .Append(" | ").Append(feature.Aggregation)
                        .Append(" | ").Append(owner == type ? "_this type_" : MarkdownText.EscapeTableCell(this.TypeLink(owner.Type)))
                        .Append(" |\n");
                }
            }

            builder.Append('\n');
        }

        private IEnumerable<(ApiTypeModel Owner, ApiPropertyModel Property)> MappedFeatures(ApiTypeModel type)
        {
            var owners = type.InheritanceChain.Select(this.model.Find).Where(owner => owner is not null).Select(owner => owner!).ToList();
            owners.Add(type);

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<(ApiTypeModel, ApiPropertyModel)>();

            // Most-derived first so an overriding property wins, then present root-first.
            foreach (var owner in Enumerable.Reverse(owners))
            {
                foreach (var property in owner.Properties.Where(property => property.ReqIf is not null && !property.IsStatic))
                {
                    if (seen.Add(property.Name))
                    {
                        result.Add((owner, property));
                    }
                }
            }

            return result
                .OrderBy(entry => owners.IndexOf(entry.Item1))
                .ThenBy(entry => entry.Item2.Name, StringComparer.Ordinal);
        }

        private void AppendEnumLiterals(StringBuilder builder, ApiTypeModel type)
        {
            builder.Append("## Values\n\n");
            if (type.Fields.Count == 0)
            {
                builder.Append("_None._\n\n");
                return;
            }

            builder.Append("| Name | Value | Description |\n|---|---|---|\n");
            foreach (var field in type.Fields)
            {
                builder.Append("| ").Append(MarkdownText.CodeCell(field.Name))
                    .Append(" | ").Append(MarkdownText.CodeCell(field.Value ?? string.Empty))
                    .Append(" | ").Append(this.docs.RenderInline(field.Doc.Summary))
                    .Append(" |\n");
            }

            builder.Append('\n');
        }

        private void AppendProperties(StringBuilder builder, ApiTypeModel type)
        {
            if (type.Properties.Count == 0)
            {
                return;
            }

            builder.Append("## Properties\n\n| Name | Type | Get | Set | ReqIF feature | Description |\n|---|---|---|---|---|---|\n");
            foreach (var property in type.Properties)
            {
                var name = property.IsStatic ? property.Name + " (static)" : property.Name;
                var summary = property.Doc.Summary ?? property.Doc.Value;
                var feature = property.ReqIf is null ? string.Empty : MarkdownText.CodeCell(property.ReqIf.Feature) + " " + MarkdownText.EscapeTableCell("[" + property.ReqIf.Multiplicity + "]");

                builder.Append("| ").Append(MarkdownText.CodeCell(name))
                    .Append(" | ").Append(MarkdownText.EscapeTableCell(this.TypeLink(property.Property.PropertyType)))
                    .Append(" | ").Append(property.Getter ?? string.Empty)
                    .Append(" | ").Append(property.Setter ?? string.Empty)
                    .Append(" | ").Append(feature)
                    .Append(" | ").Append(this.docs.RenderInline(summary))
                    .Append(" |\n");
            }

            builder.Append('\n');
        }

        private void AppendMethodSection(StringBuilder builder, string heading, IReadOnlyList<ApiMethodModel> methods)
        {
            if (methods.Count == 0)
            {
                return;
            }

            builder.Append("## ").Append(heading).Append("\n\n");

            foreach (var method in methods)
            {
                builder.Append("### ").Append(method.Heading);
                if (method.IsExtension)
                {
                    builder.Append(" (extension)");
                }

                builder.Append("\n\n```csharp\n").Append(method.Signature).Append("\n```\n\n");

                if (method.IsExtension)
                {
                    builder.Append("Extension method on ").Append(this.TypeLink(method.ExtendedType!)).Append(".\n\n");
                }

                builder.Append(this.docs.Render(method.Doc.Summary) ?? "_No summary available._").Append("\n\n");

                if (method.Doc.InheritedFrom is not null)
                {
                    builder.Append("_Documentation inherited from ").Append(MarkdownText.Code(DocMarkdownRenderer.CrefDisplayName(method.Doc.InheritedFrom))).Append("._\n\n");
                }

                this.AppendGenericParameters(builder, method);
                this.AppendParameters(builder, method);

                if (method.ReturnType is not null && method.ReturnType != "void")
                {
                    var returns = this.docs.RenderInline(method.Doc.Returns);
                    builder.Append("**Returns:** ").Append(MarkdownText.Code(method.ReturnType));
                    builder.Append(returns.Length == 0 ? string.Empty : " - " + returns).Append("\n\n");
                }

                this.AppendExceptions(builder, method.Doc.Exceptions);
                AppendRemarks(builder, this.docs.Render(method.Doc.Remarks));
                AppendExample(builder, this.docs.Render(method.Doc.Example));
            }
        }

        private void AppendGenericParameters(StringBuilder builder, ApiMethodModel method)
        {
            if (method.TypeParameters.Count == 0)
            {
                return;
            }

            builder.Append("| Type parameter | Description |\n|---|---|\n");
            foreach (var name in method.TypeParameters)
            {
                method.Doc.TypeParameters.TryGetValue(name, out var element);
                builder.Append("| ").Append(MarkdownText.CodeCell(name)).Append(" | ").Append(this.docs.RenderInline(element)).Append(" |\n");
            }

            builder.Append('\n');
        }

        private void AppendParameters(StringBuilder builder, ApiMethodModel method)
        {
            if (method.Parameters.Count == 0)
            {
                return;
            }

            builder.Append("| Parameter | Type | Default | Description |\n|---|---|---|---|\n");
            foreach (var parameter in method.Parameters)
            {
                method.Doc.Parameters.TryGetValue(parameter.Name, out var element);
                var typeText = (parameter.Modifier is null ? string.Empty : parameter.Modifier + " ") + parameter.Type;

                builder.Append("| ").Append(MarkdownText.CodeCell(parameter.Name))
                    .Append(" | ").Append(MarkdownText.CodeCell(typeText))
                    .Append(" | ").Append(parameter.DefaultValue is null ? string.Empty : MarkdownText.CodeCell(parameter.DefaultValue))
                    .Append(" | ").Append(this.docs.RenderInline(element))
                    .Append(" |\n");
            }

            builder.Append('\n');
        }

        private void AppendExceptions(StringBuilder builder, IReadOnlyList<XElement> exceptions)
        {
            if (exceptions.Count == 0)
            {
                return;
            }

            builder.Append("| Exception | Condition |\n|---|---|\n");
            foreach (var exception in exceptions)
            {
                var cref = (string?)exception.Attribute("cref") ?? string.Empty;
                builder.Append("| ").Append(MarkdownText.CodeCell(DocMarkdownRenderer.CrefDisplayName(cref)))
                    .Append(" | ").Append(this.docs.RenderInline(exception))
                    .Append(" |\n");
            }

            builder.Append('\n');
        }

        private static void AppendRemarks(StringBuilder builder, string? remarks)
        {
            if (remarks is not null)
            {
                builder.Append("**Remarks:**\n\n").Append(remarks).Append("\n\n");
            }
        }

        private static void AppendExample(StringBuilder builder, string? example)
        {
            if (example is not null)
            {
                builder.Append("**Example:**\n\n").Append(example).Append("\n\n");
            }
        }

        private void AppendEvents(StringBuilder builder, ApiTypeModel type)
        {
            if (type.Events.Count == 0)
            {
                return;
            }

            builder.Append("## Events\n\n| Name | Handler type | Description |\n|---|---|---|\n");
            foreach (var @event in type.Events)
            {
                builder.Append("| ").Append(MarkdownText.CodeCell(@event.IsStatic ? @event.Name + " (static)" : @event.Name))
                    .Append(" | ").Append(MarkdownText.CodeCell(@event.Type))
                    .Append(" | ").Append(this.docs.RenderInline(@event.Doc.Summary))
                    .Append(" |\n");
            }

            builder.Append('\n');
        }

        private void AppendFields(StringBuilder builder, ApiTypeModel type)
        {
            if (type.Fields.Count == 0)
            {
                return;
            }

            builder.Append("## Fields\n\n| Name | Type | Value | Description |\n|---|---|---|---|\n");
            foreach (var field in type.Fields)
            {
                var modifiers = field.IsConstant ? "const " : (field.IsStatic ? "static " : string.Empty) + (field.IsReadOnly ? "readonly " : string.Empty);
                builder.Append("| ").Append(MarkdownText.CodeCell(field.Name))
                    .Append(" | ").Append(MarkdownText.CodeCell(modifiers + field.Type))
                    .Append(" | ").Append(field.Value is null ? string.Empty : MarkdownText.CodeCell(field.Value))
                    .Append(" | ").Append(this.docs.RenderInline(field.Doc.Summary))
                    .Append(" |\n");
            }

            builder.Append('\n');
        }

        private void AppendAvailableExtensions(StringBuilder builder, ApiTypeModel type)
        {
            if (type.AvailableExtensionMethods.Count == 0)
            {
                return;
            }

            builder.Append("## Extension methods available\n\n");
            builder.Append("Extension methods from the documented packages whose `this` parameter accepts ").Append(MarkdownText.Code(type.Name))
                .Append(", including those declared for its base types and interfaces. Bring the declaring class's namespace into scope with a `using` directive to call them.\n\n");
            builder.Append("| Method | Extends | Declared in | Package | Description |\n|---|---|---|---|---|\n");

            foreach (var method in type.AvailableExtensionMethods)
            {
                var declaring = this.model.Find(method.Method.DeclaringType)!;
                var link = "[" + MarkdownText.Code(method.Heading) + "](" + FileNameOf(declaring) + ")";

                builder.Append("| ").Append(MarkdownText.EscapeTableCell(link))
                    .Append(" | ").Append(MarkdownText.EscapeTableCell(this.TypeLink(method.ExtendedType!)))
                    .Append(" | ").Append(MarkdownText.EscapeTableCell(MarkdownText.Code(declaring.Namespace + "." + declaring.Name)))
                    .Append(" | ").Append(MarkdownText.EscapeTableCell(declaring.Package.PackageId))
                    .Append(" | ").Append(this.docs.RenderInline(method.Doc.Summary))
                    .Append(" |\n");
            }

            builder.Append('\n');
        }

        private void AppendInheritedMembers(StringBuilder builder, ApiTypeModel type)
        {
            if (type.Kind != "class" || type.InheritanceChain.Count == 0)
            {
                return;
            }

            var sections = new List<string>();
            var declared = new HashSet<string>(type.Properties.Select(p => p.Name).Concat(type.Methods.Select(m => m.Name)), StringComparer.Ordinal);

            foreach (var baseType in Enumerable.Reverse(type.InheritanceChain))
            {
                var baseModel = this.model.Find(baseType);
                if (baseModel is null)
                {
                    continue;
                }

                var properties = baseModel.Properties.Where(p => !p.IsStatic && declared.Add(p.Name)).Select(p => MarkdownText.Code(p.Name)).ToList();
                var methods = baseModel.Methods.Where(m => !m.IsStatic && !declared.Contains(m.Name)).Select(m => m.Name).Distinct(StringComparer.Ordinal).ToList();
                foreach (var name in methods)
                {
                    declared.Add(name);
                }

                if (properties.Count == 0 && methods.Count == 0)
                {
                    continue;
                }

                var line = new StringBuilder("- From ").Append(this.TypeLink(baseType)).Append(':');
                if (properties.Count > 0)
                {
                    line.Append(" properties ").Append(string.Join(", ", properties)).Append(methods.Count > 0 ? ";" : string.Empty);
                }

                if (methods.Count > 0)
                {
                    line.Append(" methods ").Append(string.Join(", ", methods.Select(name => MarkdownText.Code(name + "()"))));
                }

                sections.Add(line.ToString());
            }

            if (sections.Count == 0)
            {
                return;
            }

            builder.Append("## Inherited members\n\n").Append(string.Join("\n", sections)).Append("\n\n");
        }
    }
}
