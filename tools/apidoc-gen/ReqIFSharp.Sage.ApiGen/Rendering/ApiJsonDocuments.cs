// -------------------------------------------------------------------------------------------------
// <copyright file="ApiJsonDocuments.cs" company="Starion Group S.A.">
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
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;

    using ReqIFSharp.Sage.ApiGen.Documentation;
    using ReqIFSharp.Sage.ApiGen.Model;

    /// <summary>
    /// Builds the JSON documents (<c>index.json</c>, <c>mapping.json</c>, <c>api.json</c>,
    /// <c>manifest.json</c>) and the <c>index.md</c> table from an <see cref="ApiModel"/>. All JSON is written
    /// with the fixed <see cref="SerializerOptions"/>, with arrays in ordinal order, so regeneration is
    /// byte-for-byte deterministic. Documentation text in JSON is markdown without links.
    /// </summary>
    public static class ApiJsonDocuments
    {
        /// <summary>
        /// The generator name recorded in <c>manifest.json</c>.
        /// </summary>
        public const string GeneratorName = "reqifsharp-sage";

        /// <summary>
        /// The fixed <see cref="JsonSerializerOptions"/> used for every generated JSON file.
        /// </summary>
        public static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        private static readonly DocMarkdownRenderer PlainDocs = new();

        /// <summary>
        /// Serializes <c>index.json</c>: one row per type, ordered ordinally by full name.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The JSON text (LF line endings).</returns>
        public static string IndexJson(ApiModel model)
        {
            var rows = model.Types.Select(type => new
            {
                name = type.Name,
                fullName = type.FullName,
                @namespace = type.Namespace,
                assembly = type.Package.Assembly.GetName().Name,
                kind = type.Kind,
                isAbstract = type.IsAbstract,
                isStatic = type.IsStatic,
                reqifClass = type.ReqIfClass,
                xmlElement = type.XmlElement,
                file = "types/" + TypePageRenderer.FileNameOf(type),
            });

            return Serialize(rows);
        }

        /// <summary>
        /// Renders <c>index.md</c>: a table per namespace of every documented type.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The markdown text.</returns>
        public static string IndexMarkdown(ApiModel model)
        {
            var builder = new StringBuilder();
            builder.Append("# reqifsharp API index\n\n");
            builder.Append(string.Join(", ", model.Packages.Select(package => MarkdownText.Code(package.PackageId + " " + package.Version))))
                .Append(" - ").Append(model.Types.Count.ToString(CultureInfo.InvariantCulture)).Append(" public types.\n\n");

            foreach (var group in model.Types.GroupBy(type => type.Namespace).OrderBy(group => group.Key, StringComparer.Ordinal))
            {
                builder.Append("## ").Append(group.Key.Length == 0 ? "(global)" : group.Key).Append("\n\n");
                builder.Append("| Type | Kind | Abstract | Static | ReqIF class | XML element | Summary |\n|---|---|---|---|---|---|---|\n");

                foreach (var type in group.OrderBy(type => type.FullName, StringComparer.Ordinal))
                {
                    var summary = PlainDocs.RenderInline(type.Doc.Summary);
                    builder.Append("| [").Append(MarkdownText.EscapeTableCell(type.Name)).Append("](types/").Append(TypePageRenderer.FileNameOf(type)).Append(')')
                        .Append(" | ").Append(type.Kind)
                        .Append(" | ").Append(type.IsAbstract ? "yes" : string.Empty)
                        .Append(" | ").Append(type.IsStatic ? "yes" : string.Empty)
                        .Append(" | ").Append(type.ReqIfClass is null ? string.Empty : MarkdownText.CodeCell(type.ReqIfClass))
                        .Append(" | ").Append(type.XmlElement is null ? string.Empty : MarkdownText.CodeCell(type.XmlElement))
                        .Append(" | ").Append(FirstSentence(summary))
                        .Append(" |\n");
                }

                builder.Append('\n');
            }

            return builder.ToString().TrimEnd('\n') + "\n";
        }

        /// <summary>
        /// Serializes <c>mapping.json</c>: the ReqIF metaclass ↔ C# type ↔ XML element map, with the mapped
        /// features each type declares.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The JSON text.</returns>
        public static string MappingJson(ApiModel model)
        {
            var document = new
            {
                reqifSharpVersion = VersionOf(model, ApiGenerator.ReqIFSharpPackageId),
                extensionsVersion = VersionOf(model, ApiGenerator.ExtensionsPackageId),
                entries = model.Types
                    .Where(type => type.ReqIfClass is not null)
                    .OrderBy(type => type.ReqIfClass, StringComparer.Ordinal)
                    .ThenBy(type => type.FullName, StringComparer.Ordinal)
                    .Select(type => new
                    {
                        reqifClass = type.ReqIfClass,
                        csharpType = type.FullName,
                        xmlElement = type.XmlElement,
                        isAbstract = type.IsAbstract,
                        superClass = type.BaseType is null ? null : model.Find(type.BaseType)?.ReqIfClass,
                        properties = type.Properties
                            .Where(property => property.ReqIf is not null)
                            .Select(property => new
                            {
                                reqifFeature = property.ReqIf!.Feature,
                                xmlName = property.ReqIf.XmlName,
                                csharpProperty = property.Name,
                                csharpType = CSharpTypeNames.Format(property.Property.PropertyType, qualified: true),
                                multiplicity = property.ReqIf.Multiplicity,
                                aggregation = property.ReqIf.Aggregation,
                                isOrdered = property.ReqIf.IsOrdered,
                                isUnique = property.ReqIf.IsUnique,
                            }),
                    }),
            };

            return Serialize(document);
        }

        /// <summary>
        /// Serializes <c>manifest.json</c>: the documented packages and the generator name.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The JSON text.</returns>
        public static string ManifestJson(ApiModel model)
        {
            var document = new
            {
                packages = model.Packages.Select(package => new
                {
                    id = package.PackageId,
                    version = package.Version,
                    assemblyVersion = package.AssemblyVersion,
                }),
                generator = GeneratorName,
            };

            return Serialize(document);
        }

        /// <summary>
        /// Serializes <c>api.json</c>: the full machine-readable model of every type and member.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The JSON text.</returns>
        public static string ApiJson(ApiModel model)
        {
            var document = new
            {
                packages = model.Packages.Select(package => new { id = package.PackageId, version = package.Version, assemblyVersion = package.AssemblyVersion }),
                types = model.Types.Select(type => new
                {
                    name = type.Name,
                    fullName = type.FullName,
                    @namespace = type.Namespace,
                    assembly = type.Package.Assembly.GetName().Name,
                    package = type.Package.PackageId,
                    packageVersion = type.Package.Version,
                    kind = type.Kind,
                    isAbstract = type.IsAbstract,
                    isStatic = type.IsStatic,
                    isSealed = type.IsSealed,
                    declaration = type.Declaration,
                    baseType = type.BaseType is null ? null : CSharpTypeNames.Format(type.BaseType, qualified: true),
                    inheritanceChain = type.InheritanceChain.Select(t => CSharpTypeNames.Format(t, qualified: true)),
                    interfaces = type.Interfaces.Select(t => CSharpTypeNames.Format(t, qualified: true)),
                    derivedTypes = type.DerivedTypes.Select(t => t.FullName),
                    reqifClass = type.ReqIfClass,
                    xmlElement = type.XmlElement,
                    reqifNote = type.ReqIfNote,
                    docId = type.DocId,
                    summary = PlainDocs.Render(type.Doc.Summary),
                    remarks = PlainDocs.Render(type.Doc.Remarks),
                    file = "types/" + TypePageRenderer.FileNameOf(type),
                    constructors = type.Constructors.Select(Method),
                    properties = type.Properties.Select(property => new
                    {
                        name = property.Name,
                        type = property.Type,
                        qualifiedType = CSharpTypeNames.Format(property.Property.PropertyType, qualified: true),
                        getter = property.Getter,
                        setter = property.Setter,
                        isStatic = property.IsStatic,
                        isAbstract = property.IsAbstract,
                        reqif = property.ReqIf is null ? null : new
                        {
                            feature = property.ReqIf.Feature,
                            xmlName = property.ReqIf.XmlName,
                            multiplicity = property.ReqIf.Multiplicity,
                            lower = property.ReqIf.LowerValue,
                            upper = property.ReqIf.UpperValue is < 0 or int.MaxValue ? -1 : property.ReqIf.UpperValue,
                            aggregation = property.ReqIf.Aggregation,
                            isOrdered = property.ReqIf.IsOrdered,
                            isUnique = property.ReqIf.IsUnique,
                            isReadOnly = property.ReqIf.IsReadOnly,
                            isDerived = property.ReqIf.IsDerived,
                            isDerivedUnion = property.ReqIf.IsDerivedUnion,
                            defaultValue = property.ReqIf.DefaultValue,
                        },
                        docId = property.DocId,
                        summary = PlainDocs.Render(property.Doc.Summary ?? property.Doc.Value),
                    }),
                    methods = type.Methods.Select(Method),
                    events = type.Events.Select(@event => new
                    {
                        name = @event.Name,
                        type = @event.Type,
                        isStatic = @event.IsStatic,
                        docId = @event.DocId,
                        summary = PlainDocs.Render(@event.Doc.Summary),
                    }),
                    fields = type.Fields.Select(field => new
                    {
                        name = field.Name,
                        type = field.Type,
                        isStatic = field.IsStatic,
                        isConstant = field.IsConstant,
                        isReadOnly = field.IsReadOnly,
                        value = field.Value,
                        docId = field.DocId,
                        summary = PlainDocs.Render(field.Doc.Summary),
                    }),
                    extensionMethodsAvailable = type.AvailableExtensionMethods.Select(method => new
                    {
                        declaringType = XmlDocIds.TypeDefinitionName(method.Method.DeclaringType!),
                        name = method.Name,
                        signature = method.Signature,
                        extendedType = CSharpTypeNames.Format(method.ExtendedType!, qualified: true),
                        docId = method.DocId,
                    }),
                }),
            };

            return Serialize(document);
        }

        private static object Method(ApiMethodModel method)
        {
            return new
            {
                name = method.Name,
                heading = method.Heading,
                signature = method.Signature,
                returnType = method.ReturnType,
                isStatic = method.IsStatic,
                isAbstract = method.IsAbstract,
                isVirtual = method.IsVirtual,
                isExtension = method.IsExtension,
                extendedType = method.ExtendedType is null ? null : CSharpTypeNames.Format(method.ExtendedType, qualified: true),
                typeParameters = method.TypeParameters,
                parameters = method.Parameters.Select(parameter => new
                {
                    name = parameter.Name,
                    type = parameter.Type,
                    qualifiedType = CSharpTypeNames.Format(parameter.Parameter.ParameterType, qualified: true),
                    modifier = parameter.Modifier,
                    isOptional = parameter.IsOptional,
                    defaultValue = parameter.DefaultValue,
                    description = method.Doc.Parameters.TryGetValue(parameter.Name, out var element) ? PlainDocs.Render(element) : null,
                }),
                docId = method.DocId,
                summary = PlainDocs.Render(method.Doc.Summary),
                returns = PlainDocs.Render(method.Doc.Returns),
                remarks = PlainDocs.Render(method.Doc.Remarks),
                exceptions = method.Doc.Exceptions.Select(exception => new
                {
                    type = ExceptionType((string?)exception.Attribute("cref")),
                    condition = PlainDocs.Render(exception),
                }),
                inheritedDocFrom = method.Doc.InheritedFrom,
            };
        }

        private static string? ExceptionType(string? cref)
        {
            if (cref is null)
            {
                return null;
            }

            return cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
        }

        private static string? VersionOf(ApiModel model, string packageId)
        {
            return model.Packages.FirstOrDefault(package => string.Equals(package.PackageId, packageId, StringComparison.Ordinal))?.Version;
        }

        private static string FirstSentence(string text)
        {
            var end = text.IndexOf(". ", StringComparison.Ordinal);
            return end < 0 ? text : text[..(end + 1)];
        }

        private static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, SerializerOptions).Replace("\r\n", "\n") + "\n";
        }
    }
}
