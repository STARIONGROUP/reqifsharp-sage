// -------------------------------------------------------------------------------------------------
// <copyright file="InspectCommand.cs" company="Starion Group S.A.">
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
    using System.CommandLine;
    using System.IO;
    using System.Linq;

    using ReqIFSharp.Sage.Knowledge;
    using ReqIFSharp.Sage.Tools.Inspection;

    using Spectre.Console;

    /// <summary>
    /// <c>reqifsharp-sage inspect</c>: checks a <c>.reqif</c> or <c>.reqifz</c> file against the ReqIF
    /// schema, the metamodel rules and, when a guide is imported, the Implementation Guide's recommendations.
    /// </summary>
    public static class InspectCommand
    {
        private static readonly Argument<string> FileArgument = new("file")
        {
            Description = "The .reqif or .reqifz file to inspect.",
        };

        private static readonly Option<bool> NoGuideRulesOption = new("--no-guide-rules")
        {
            Description = "Skip the implementation guide checks (G-xxx) even when a guide is imported.",
        };

        private static readonly Option<bool> GuideRulesOption = new("--guide-rules")
        {
            Description = "Run the implementation guide checks (G-xxx) even when no guide is imported.",
        };

        private static readonly Option<bool> RulesOption = new("--rules")
        {
            Description = "List every rule the inspector knows instead of inspecting a file.",
        };

        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command Build()
        {
            var command = new Command("inspect", "Check a .reqif/.reqifz file: XSD validation, ReqIF metamodel rules and implementation guide recommendations.");
            FileArgument.Arity = ArgumentArity.ZeroOrOne;
            command.Add(FileArgument);
            command.Add(GlobalOptions.RepositoryRoot);
            command.Add(GlobalOptions.Json);
            command.Add(NoGuideRulesOption);
            command.Add(GuideRulesOption);
            command.Add(RulesOption);

            command.SetAction(parseResult =>
            {
                var json = parseResult.GetValue(GlobalOptions.Json);

                if (parseResult.GetValue(RulesOption))
                {
                    WriteRules(json);
                    return 0;
                }

                var file = parseResult.GetValue(FileArgument);
                if (string.IsNullOrWhiteSpace(file))
                {
                    AnsiConsole.MarkupLine("[red]Pass a .reqif or .reqifz file, or --rules.[/]");
                    return 2;
                }

                var path = Path.GetFullPath(file);
                if (!File.Exists(path))
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{path} does not exist.[/]");
                    return 2;
                }

                var layout = GlobalOptions.LayoutFrom(parseResult);
                var guide = new InstalledVersionsStore(layout.KnowledgeRoot).Load().Guides
                    .Where(g => g.Imported)
                    .OrderBy(g => g.Version, StringComparer.Ordinal)
                    .LastOrDefault();

                var includeGuideRules = !parseResult.GetValue(NoGuideRulesOption) && (guide is not null || parseResult.GetValue(GuideRulesOption));
                var report = new ReqIFInspector().Inspect(path, new InspectionOptions(includeGuideRules, guide?.Version ?? InspectionRuleCatalog.GuideVersion));

                if (json)
                {
                    GlobalOptions.WriteJson(new
                    {
                        report.File,
                        GuideRulesApplied = includeGuideRules,
                        report.ErrorCount,
                        report.WarningCount,
                        report.InfoCount,
                        report.Documents,
                        report.Findings,
                    });
                }
                else
                {
                    WriteReport(report, includeGuideRules);
                }

                return report.ErrorCount > 0 ? 1 : 0;
            });

            return command;
        }

        private static void WriteReport(InspectionReport report, bool includeGuideRules)
        {
            AnsiConsole.MarkupLineInterpolated($"[bold]{report.File}[/]: {report.ErrorCount} error(s), {report.WarningCount} warning(s), {report.InfoCount} info{(includeGuideRules ? string.Empty : " (guide rules not applied)")}");
            foreach (var document in report.Documents)
            {
                AnsiConsole.MarkupLineInterpolated($"  {document.Document ?? "document"}: '{document.Title}' ReqIF {document.ReqIFVersion}, tool {document.SourceToolId}, {document.SpecObjectCount} objects, {document.SpecificationCount} specifications, {document.SpecRelationCount} relations");
            }

            if (report.Findings.Count == 0)
            {
                return;
            }

            var table = new Table().AddColumns("Severity", "Rule", "Where", "Message", "Reference");
            foreach (var finding in report.Findings)
            {
                var where = string.Join(" ", new[] { finding.Document, finding.Line is null ? null : $"line {finding.Line}", finding.Identifier }.Where(s => !string.IsNullOrEmpty(s)));
                table.AddRow(Markup.Escape(finding.Severity), Markup.Escape(finding.RuleId), Markup.Escape(where), Markup.Escape(finding.Message), Markup.Escape(finding.Reference ?? string.Empty));
            }

            AnsiConsole.Write(table);
        }

        private static void WriteRules(bool json)
        {
            if (json)
            {
                GlobalOptions.WriteJson(InspectionRuleCatalog.All);
                return;
            }

            var table = new Table().AddColumns("Rule", "Severity", "Category", "Reference", "Checks");
            foreach (var rule in InspectionRuleCatalog.All)
            {
                table.AddRow(Markup.Escape(rule.Id), Markup.Escape(rule.Severity), Markup.Escape(rule.Category), Markup.Escape(rule.Reference ?? string.Empty), Markup.Escape(rule.Description));
            }

            AnsiConsole.Write(table);
        }
    }
}
