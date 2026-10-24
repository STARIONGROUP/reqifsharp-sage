// -------------------------------------------------------------------------------------------------
// <copyright file="CheckCommand.cs" company="Starion Group S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.Linq;

    using ReqIFSharp.Sage.Knowledge;

    using Spectre.Console;

    /// <summary>
    /// <c>reqifsharp-sage check</c>: an offline status report of what is fetched, imported and generated.
    /// The SessionStart hook runs it with <c>--json</c> every session.
    /// </summary>
    public static class CheckCommand
    {
        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command Build()
        {
            var command = new Command("check", "Report what is fetched, imported and generated on this machine (offline).");
            command.Add(GlobalOptions.RepositoryRoot);
            command.Add(GlobalOptions.Json);

            command.SetAction(parseResult =>
            {
                var layout = GlobalOptions.LayoutFrom(parseResult);
                var report = BuildReport(layout);

                if (parseResult.GetValue(GlobalOptions.Json))
                {
                    GlobalOptions.WriteJson(report);
                    return 0;
                }

                foreach (var line in report.Summary)
                {
                    AnsiConsole.WriteLine(line);
                }

                return 0;
            });

            return command;
        }

        /// <summary>
        /// Builds the status report for <paramref name="layout"/>.
        /// </summary>
        /// <param name="layout">The repository layout.</param>
        /// <returns>The report.</returns>
        public static CheckReport BuildReport(RepositoryLayout layout)
        {
            var manifest = new InstalledVersionsStore(layout.KnowledgeRoot).Load();
            var defaultEntry = manifest.Versions.FirstOrDefault(v => v.Version == manifest.Default);
            var apiUpToDate = manifest.Api is not null && manifest.Api.ReqIFSharpVersion == BundledPackages.ReqIFSharpVersion;
            var summary = new List<string>();

            if (defaultEntry is null)
            {
                summary.Add($"No ReqIF version fetched yet. Run 'reqifsharp-sage fetch --version {KnownReqIFVersions.Current.Version}' then 'reqifsharp-sage generate'.");
            }
            else if (!defaultEntry.Generated)
            {
                summary.Add($"ReqIF {defaultEntry.Version} is fetched but not generated. Run 'reqifsharp-sage generate --version {defaultEntry.Version}'.");
            }
            else
            {
                summary.Add(defaultEntry.SpecGenerated
                    ? $"ReqIF {defaultEntry.Version} is ready, including verbatim specification citation."
                    : $"ReqIF {defaultEntry.Version} metamodel and schema are ready; specification citation is limited to clause numbers (PDF not fetched or Python unavailable).");
            }

            var guide = manifest.Guides.Where(g => g.Imported).OrderBy(g => g.Version, StringComparer.Ordinal).LastOrDefault();
            if (guide is null)
            {
                summary.Add("The prostep ivip Implementation Guide is not imported (optional). If you have a copy, run 'reqifsharp-sage import-guide <pdf>'.");
            }
            else if (!guide.Generated)
            {
                summary.Add($"Implementation guide {guide.Version} is imported but its text is not extracted. Run 'reqifsharp-sage generate'.");
            }
            else
            {
                summary.Add($"Implementation guide {guide.Version} is ready.");
            }

            if (manifest.Api is null)
            {
                summary.Add("The reqifsharp API reference is not generated. Run 'reqifsharp-sage generate'.");
            }
            else if (!apiUpToDate)
            {
                summary.Add($"The reqifsharp API reference describes ReqIFSharp {manifest.Api.ReqIFSharpVersion}, this CLI bundles {BundledPackages.ReqIFSharpVersion}. Run 'reqifsharp-sage generate' to refresh it.");
            }
            else
            {
                summary.Add($"The reqifsharp API reference (ReqIFSharp {manifest.Api.ReqIFSharpVersion}) is ready.");
            }

            return new CheckReport(
                manifest.Default,
                manifest.Versions.Count > 0,
                manifest.Versions,
                manifest.Guides,
                manifest.Api,
                apiUpToDate,
                BundledPackages.ReqIFSharpVersion,
                BundledPackages.CliVersion,
                layout.RepositoryRoot,
                Environment.ProcessPath,
                summary);
        }
    }

    /// <summary>
    /// The <c>check --json</c> output. The SessionStart hook deserializes it, so field names are a contract.
    /// </summary>
    /// <param name="Default">The default ReqIF version, if any.</param>
    /// <param name="HasAnyVersion">Whether any ReqIF version is recorded.</param>
    /// <param name="Versions">The ReqIF versions.</param>
    /// <param name="Guides">The implementation guides.</param>
    /// <param name="Api">The generated reqifsharp API reference, if any.</param>
    /// <param name="ApiUpToDate">Whether the API reference describes the bundled ReqIFSharp version.</param>
    /// <param name="BundledReqIFSharpVersion">The ReqIFSharp version this CLI bundles.</param>
    /// <param name="CliVersion">This CLI's version.</param>
    /// <param name="RepositoryRoot">The plugin root the report is about.</param>
    /// <param name="CliPath">The path of the running CLI executable.</param>
    /// <param name="Summary">Human-readable status lines.</param>
    public sealed record CheckReport(
        string? Default,
        bool HasAnyVersion,
        IReadOnlyList<InstalledVersion> Versions,
        IReadOnlyList<InstalledGuide> Guides,
        InstalledApi? Api,
        bool ApiUpToDate,
        string BundledReqIFSharpVersion,
        string CliVersion,
        string RepositoryRoot,
        string? CliPath,
        IReadOnlyList<string> Summary);
}
