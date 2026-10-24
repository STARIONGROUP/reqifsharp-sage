// -------------------------------------------------------------------------------------------------
// <copyright file="MetamodelGenerator.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;

    using ReqIFSharp.Sage.MetamodelGen.Cmof;
    using ReqIFSharp.Sage.MetamodelGen.Json;
    using ReqIFSharp.Sage.MetamodelGen.Rendering;
    using ReqIFSharp.Sage.MetamodelGen.Xsd;

    /// <summary>
    /// Generates the <c>metamodel/</c> and <c>schema/</c> parts of a ReqIF version's knowledge base from
    /// the OMG CMOF metamodel and XML schema files.
    /// </summary>
    public static class MetamodelGenerator
    {
        /// <summary>
        /// Reads the metamodel (and, when given, the schema files) and writes <c>metamodel/</c> and
        /// <c>schema/</c> under <paramref name="knowledgeOutputDirectory"/>.
        /// </summary>
        /// <param name="cmofPath">The <c>reqif.cmof</c> file.</param>
        /// <param name="reqifXsdPath">The <c>reqif.xsd</c> file, or null to generate the metamodel without XML serialization details.</param>
        /// <param name="driverXsdPath">The <c>driver.xsd</c> file (the XHTML subset), or null.</param>
        /// <param name="knowledgeOutputDirectory">The version's knowledge root, e.g. <c>knowledge/1.2</c>.</param>
        /// <returns>What was generated.</returns>
        public static MetamodelGenerationResult Generate(string cmofPath, string? reqifXsdPath, string? driverXsdPath, string knowledgeOutputDirectory)
        {
            var model = CmofReader.Read(cmofPath);
            var schema = reqifXsdPath is not null && File.Exists(reqifXsdPath) ? XsdReader.Read(reqifXsdPath) : null;
            var driver = driverXsdPath is not null && File.Exists(driverXsdPath) ? XsdReader.Read(driverXsdPath) : null;
            var view = new MetamodelView(model, schema);

            var metamodelDirectory = Path.Combine(knowledgeOutputDirectory, "metamodel");
            foreach (var @class in model.Classes)
            {
                KnowledgeFileWriter.WriteText(Path.Combine(metamodelDirectory, "elements", $"{@class.Name}.md"), ElementPageRenderer.RenderClass(view, @class));
            }

            foreach (var primitiveType in model.PrimitiveTypes)
            {
                KnowledgeFileWriter.WriteText(Path.Combine(metamodelDirectory, "elements", $"{primitiveType.Name}.md"), ElementPageRenderer.RenderPrimitiveType(primitiveType));
            }

            var index = MetamodelJson.BuildIndex(view);
            KnowledgeFileWriter.WriteJson(Path.Combine(metamodelDirectory, "index.json"), index);
            KnowledgeFileWriter.WriteText(Path.Combine(metamodelDirectory, "index.md"), RenderMetamodelIndex(index));
            KnowledgeFileWriter.WriteJson(Path.Combine(metamodelDirectory, "metamodel.json"), MetamodelJson.Build(view));

            var schemaTypeCount = 0;
            if (schema is not null)
            {
                schemaTypeCount = GenerateSchema(view, schema, driver, Path.Combine(knowledgeOutputDirectory, "schema"));
            }

            return new MetamodelGenerationResult(model.Classes.Count, model.PrimitiveTypes.Count, schemaTypeCount, driver is not null);
        }

        private static int GenerateSchema(MetamodelView view, XsdSchema schema, XsdSchema? driver, string schemaDirectory)
        {
            foreach (var complexType in schema.ComplexTypes)
            {
                KnowledgeFileWriter.WriteText(Path.Combine(schemaDirectory, "types", $"{complexType.Name}.md"), SchemaPageRenderer.RenderComplexType(view, complexType, schema));
            }

            foreach (var simpleType in schema.SimpleTypes)
            {
                KnowledgeFileWriter.WriteText(Path.Combine(schemaDirectory, "types", $"{simpleType.Name}.md"), SchemaPageRenderer.RenderSimpleType(simpleType, schema));
            }

            if (driver is not null)
            {
                KnowledgeFileWriter.WriteText(Path.Combine(schemaDirectory, "xhtml-subset.md"), SchemaPageRenderer.RenderXhtmlSubset(driver));
            }

            var rows = schema.ComplexTypes
                .Select(t => new SchemaIndexRow(t.Name, "complexType", view.ClassOf(t)?.Name, schema.GlobalElements.FirstOrDefault(e => e.Type == t.Name)?.Name, $"types/{t.Name}.md"))
                .Concat(schema.SimpleTypes.Select(t => new SchemaIndexRow(t.Name, "simpleType", null, null, $"types/{t.Name}.md")))
                .OrderBy(r => r.Name, StringComparer.Ordinal)
                .ToList();

            KnowledgeFileWriter.WriteJson(Path.Combine(schemaDirectory, "index.json"), rows);

            var builder = new StringBuilder();
            builder.Append("# ReqIF XML schema types\n\n");
            builder.Append("Target namespace: `").Append(schema.TargetNamespace).Append("`.\n\n");
            builder.Append("| Type | Kind | Metamodel class | Global element |\n|---|---|---|---|\n");
            foreach (var row in rows)
            {
                builder.Append("| [").Append(row.Name).Append("](").Append(row.File).Append(") | ").Append(row.Kind).Append(" | ")
                    .Append(row.MetamodelClass is null ? string.Empty : $"[{row.MetamodelClass}](../metamodel/elements/{row.MetamodelClass}.md)").Append(" | ")
                    .Append(row.GlobalElement is null ? string.Empty : $"`<{row.GlobalElement}>`").Append(" |\n");
            }

            if (driver is not null)
            {
                builder.Append("\nThe XHTML allowed in `XHTML-CONTENT` is described in [xhtml-subset.md](xhtml-subset.md).\n");
            }

            KnowledgeFileWriter.WriteText(Path.Combine(schemaDirectory, "index.md"), builder.ToString());

            return rows.Count;
        }

        private static string RenderMetamodelIndex(System.Collections.Generic.IReadOnlyList<MetamodelIndexRow> rows)
        {
            var builder = new StringBuilder();
            builder.Append("# ReqIF metamodel\n\n");
            builder.Append("| Name | Kind | Package | Abstract | XML element |\n|---|---|---|---|---|\n");
            foreach (var row in rows)
            {
                builder.Append("| [").Append(row.Name).Append("](").Append(row.File).Append(") | ").Append(row.Kind).Append(" | ").Append(row.Package)
                    .Append(" | ").Append(row.IsAbstract ? "yes" : string.Empty).Append(" | ")
                    .Append(row.XmlElement is null ? string.Empty : $"`<{row.XmlElement}>`").Append(" |\n");
            }

            return builder.ToString();
        }
    }

    /// <summary>
    /// What <see cref="MetamodelGenerator.Generate"/> produced.
    /// </summary>
    /// <param name="ClassCount">The number of class pages.</param>
    /// <param name="PrimitiveTypeCount">The number of primitive type pages.</param>
    /// <param name="SchemaTypeCount">The number of schema type pages (0 when no schema was given).</param>
    /// <param name="XhtmlSubsetGenerated">Whether the XHTML subset page was generated.</param>
    public sealed record MetamodelGenerationResult(int ClassCount, int PrimitiveTypeCount, int SchemaTypeCount, bool XhtmlSubsetGenerated);
}
