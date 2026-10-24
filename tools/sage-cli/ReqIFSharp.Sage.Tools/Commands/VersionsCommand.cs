// -------------------------------------------------------------------------------------------------
// <copyright file="VersionsCommand.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Commands
{
    using System.CommandLine;
    using System.Linq;

    using ReqIFSharp.Sage.Knowledge;

    using Spectre.Console;

    /// <summary>
    /// <c>reqifsharp-sage versions</c>: lists the known ReqIF and implementation guide versions and
    /// their local state.
    /// </summary>
    public static class VersionsCommand
    {
        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command Build()
        {
            var command = new Command("versions", "List the known ReqIF and implementation guide versions and their local state.");
            command.Add(GlobalOptions.RepositoryRoot);
            command.Add(GlobalOptions.Json);

            command.SetAction(parseResult =>
            {
                var layout = GlobalOptions.LayoutFrom(parseResult);
                var manifest = new InstalledVersionsStore(layout.KnowledgeRoot).Load();

                var reqif = KnownReqIFVersions.All.Select(v =>
                {
                    var entry = manifest.Versions.FirstOrDefault(i => i.Version == v.Version);
                    return new
                    {
                        v.Version,
                        v.OmgDocumentId,
                        IsDefault = manifest.Default == v.Version,
                        Fetched = entry?.Fetched ?? false,
                        Generated = entry?.Generated ?? false,
                        SpecGenerated = entry?.SpecGenerated ?? false,
                    };
                }).ToList();

                var guides = KnownGuideVersions.All.Select(g => g.Version)
                    .Union(manifest.Guides.Select(g => g.Version))
                    .Select(version =>
                    {
                        var entry = manifest.Guides.FirstOrDefault(g => g.Version == version);
                        return new { Version = version, Imported = entry?.Imported ?? false, Generated = entry?.Generated ?? false };
                    })
                    .ToList();

                if (parseResult.GetValue(GlobalOptions.Json))
                {
                    GlobalOptions.WriteJson(new { ReqIF = reqif, Guides = guides, manifest.Api, BundledReqIFSharpVersion = BundledPackages.ReqIFSharpVersion });
                    return 0;
                }

                var table = new Table().AddColumns("ReqIF", "OMG document", "Default", "Fetched", "Generated", "Spec text");
                foreach (var row in reqif)
                {
                    table.AddRow(row.Version, row.OmgDocumentId, Mark(row.IsDefault), Mark(row.Fetched), Mark(row.Generated), Mark(row.SpecGenerated));
                }

                AnsiConsole.Write(table);

                var guideTable = new Table().AddColumns("Implementation guide", "Imported", "Text extracted");
                foreach (var row in guides)
                {
                    guideTable.AddRow(row.Version, Mark(row.Imported), Mark(row.Generated));
                }

                AnsiConsole.Write(guideTable);
                AnsiConsole.MarkupLineInterpolated($"reqifsharp API reference: {(manifest.Api is null ? "not generated" : $"ReqIFSharp {manifest.Api.ReqIFSharpVersion}")} (CLI bundles {BundledPackages.ReqIFSharpVersion})");
                return 0;
            });

            return command;
        }

        private static string Mark(bool value) => value ? "yes" : "-";
    }
}
