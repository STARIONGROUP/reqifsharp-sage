// -------------------------------------------------------------------------------------------------
// <copyright file="DataPackageGenerator.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Builds <c>knowledge/&lt;version&gt;/datapackage.json</c>: a Frictionless Data Package manifest
    /// describing the tabular indexes (metamodel, schema and specification clause index) of one ReqIF
    /// version. Markdown pages are not described; their layout is documented in <c>knowledge/README.md</c>.
    /// </summary>
    public static class DataPackageGenerator
    {
        private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

        /// <summary>
        /// Builds the manifest, listing only the indexes that exist under <paramref name="knowledgeVersionDirectory"/>.
        /// </summary>
        /// <param name="knowledgeVersionDirectory">The version's knowledge root, e.g. <c>knowledge/1.2</c>.</param>
        /// <param name="descriptor">The ReqIF version.</param>
        /// <returns>The JSON text.</returns>
        public static string Generate(string knowledgeVersionDirectory, ReqIFVersionDescriptor descriptor)
        {
            var version = descriptor.Version;
            var sources = new JsonArray(
                new JsonObject
                {
                    ["title"] = $"OMG Requirements Interchange Format (ReqIF), Version {version}",
                    ["path"] = descriptor.FileFor(SourceFileRole.SpecificationPdf).Url,
                    ["note"] = $"{descriptor.OmgDocumentId}. Not redistributed; see NOTICE.",
                },
                new JsonObject
                {
                    ["title"] = "ReqIF metamodel (CMOF)",
                    ["path"] = descriptor.FileFor(SourceFileRole.Metamodel).Url,
                    ["note"] = "Not redistributed; fetched at runtime.",
                },
                new JsonObject
                {
                    ["title"] = "ReqIF XML schema",
                    ["path"] = descriptor.FileFor(SourceFileRole.Schema).Url,
                    ["note"] = "Not redistributed; fetched at runtime.",
                });

            var resources = new JsonArray();
            AddIfPresent(resources, knowledgeVersionDirectory, "metamodel-index", "metamodel/index.json", "name", ("name", "string", true), ("kind", "string", false), ("package", "string", false), ("qualifiedName", "string", false), ("isAbstract", "boolean", false), ("xmlElement", "string", false), ("file", "string", false));
            AddIfPresent(resources, knowledgeVersionDirectory, "schema-index", "schema/index.json", "name", ("name", "string", true), ("kind", "string", false), ("metamodelClass", "string", false), ("globalElement", "string", false), ("file", "string", false));
            AddIfPresent(resources, knowledgeVersionDirectory, "spec-clause-index", "spec/index.json", "clause", ("clause", "string", true), ("title", "string", false), ("document", "string", false), ("pages", "string", false), ("normative", "boolean", false), ("file", "string", false), ("license", "string", false), ("redistributable", "boolean", false));

            var document = new JsonObject
            {
                ["profile"] = "data-package",
                ["name"] = $"reqifsharp-sage-knowledge-{version.Replace('.', '-')}",
                ["title"] = $"reqifsharp-sage knowledge base indexes - OMG ReqIF {version}",
                ["version"] = version,
                ["licenses"] = new JsonArray(
                    new JsonObject
                    {
                        ["name"] = "Apache-2.0",
                        ["path"] = "https://www.apache.org/licenses/LICENSE-2.0",
                        ["title"] = "Applies to the structure of these indexes, not to the OMG text they point to.",
                    }),
                ["sources"] = sources,
                ["resources"] = resources,
            };

            return document.ToJsonString(SerializerOptions);
        }

        private static void AddIfPresent(JsonArray resources, string root, string name, string path, string primaryKey, params (string Name, string Type, bool Required)[] fields)
        {
            if (!File.Exists(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))))
            {
                return;
            }

            var fieldsArray = new JsonArray();
            foreach (var (fieldName, type, required) in fields)
            {
                var field = new JsonObject { ["name"] = fieldName, ["type"] = type };
                if (required)
                {
                    field["constraints"] = new JsonObject { ["required"] = true };
                }

                fieldsArray.Add(field);
            }

            resources.Add(new JsonObject
            {
                ["name"] = name,
                ["path"] = path,
                ["profile"] = "tabular-data-resource",
                ["schema"] = new JsonObject { ["fields"] = fieldsArray, ["primaryKey"] = primaryKey },
            });
        }
    }
}
