// -------------------------------------------------------------------------------------------------
// <copyright file="GenerateCommand.cs" company="Starion Group S.A.">
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
    using System.IO;
    using System.Net.Http;

    using ReqIFSharp.Sage.Knowledge;
    using ReqIFSharp.Sage.Knowledge.Toolchain;

    using Spectre.Console;

    /// <summary>
    /// <c>reqifsharp-sage generate</c>: builds the knowledge base of a fetched ReqIF version, every
    /// imported implementation guide, and the reqifsharp API reference.
    /// </summary>
    public static class GenerateCommand
    {
        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command Build()
        {
            var command = new Command("generate", "Generate the knowledge base (metamodel, schema, spec text, guide text, reqifsharp API) from fetched/imported sources.");
            command.Add(GlobalOptions.RepositoryRoot);
            command.Add(GlobalOptions.Version);

            command.SetAction(async (parseResult, cancellationToken) =>
            {
                if (!GlobalOptions.TryResolveVersion(parseResult.GetValue(GlobalOptions.Version)!, out var descriptor))
                {
                    return 2;
                }

                var layout = GlobalOptions.LayoutFrom(parseResult);

                using var httpClient = new HttpClient();
                var service = new KnowledgeGenerationService(new SourceFetcher(httpClient), new PythonSpecExtractRunner(new ProcessRunner(), new UvProvisioner(httpClient)));

                AnsiConsole.MarkupLineInterpolated($"Generating the ReqIF {descriptor.Version} knowledge base...");

                GenerateOutcome outcome;
                try
                {
                    outcome = await service.GenerateAsync(layout, descriptor, cancellationToken);
                }
                catch (FileNotFoundException exception)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
                    return 1;
                }

                AnsiConsole.MarkupLineInterpolated($"  [green]metamodel[/] {outcome.Metamodel.ClassCount} classes, {outcome.Metamodel.PrimitiveTypeCount} primitive types");
                if (outcome.Metamodel.SchemaTypeCount > 0)
                {
                    AnsiConsole.MarkupLineInterpolated($"  [green]schema[/] {outcome.Metamodel.SchemaTypeCount} types{(outcome.Metamodel.XhtmlSubsetGenerated ? " + XHTML subset" : string.Empty)}");
                }
                else
                {
                    AnsiConsole.MarkupLine("  [yellow]schema[/] skipped - reqif.xsd not fetched");
                }

                WriteExtraction("spec", outcome.Specification);
                foreach (var guide in outcome.Guides)
                {
                    WriteExtraction($"implementation guide {guide.Version}", guide.Extraction);
                }

                if (outcome.Api.Succeeded)
                {
                    AnsiConsole.MarkupLineInterpolated($"  [green]reqifsharp API[/] {outcome.Api.TypeCount} types (ReqIFSharp {outcome.Api.ReqIFSharpVersion})");
                    foreach (var warning in outcome.Api.Warnings)
                    {
                        AnsiConsole.MarkupLineInterpolated($"    [yellow]warning[/] {warning}");
                    }
                }
                else
                {
                    AnsiConsole.MarkupLineInterpolated($"  [yellow]reqifsharp API[/] skipped - {outcome.Api.SkippedReason}");
                }

                AnsiConsole.MarkupLine("[green]Done.[/]");
                return 0;
            });

            return command;
        }

        private static void WriteExtraction(string label, SpecExtractionOutcome outcome)
        {
            if (outcome.Succeeded)
            {
                AnsiConsole.MarkupLineInterpolated($"  [green]{label}[/] clause text extracted");
            }
            else
            {
                AnsiConsole.MarkupLineInterpolated($"  [yellow]{label}[/] skipped - {outcome.SkippedReason}");
            }
        }
    }
}
