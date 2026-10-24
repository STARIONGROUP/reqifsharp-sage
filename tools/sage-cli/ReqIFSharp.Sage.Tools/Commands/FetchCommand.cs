// -------------------------------------------------------------------------------------------------
// <copyright file="FetchCommand.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Commands
{
    using System;
    using System.CommandLine;
    using System.IO;
    using System.Net.Http;

    using ReqIFSharp.Sage.Knowledge;
    using ReqIFSharp.Sage.Knowledge.Toolchain;

    using Spectre.Console;

    /// <summary>
    /// <c>reqifsharp-sage fetch</c>: stores a ReqIF version's OMG files (metamodel, schemas and,
    /// unless <c>--no-specs</c>, the specification PDF) under <c>sources/&lt;version&gt;/</c>.
    /// </summary>
    public static class FetchCommand
    {
        private static readonly Option<bool> NoSpecsOption = new("--no-specs")
        {
            Description = "Skip the specification PDF (metamodel, schema and API knowledge still work; spec citation is limited to clause numbers).",
        };

        private static readonly Option<bool> NoDefaultOption = new("--no-default")
        {
            Description = "Do not make this version the default.",
        };

        private static readonly Option<string?> FromOption = new("--from")
        {
            Description = "Take the files from this local directory instead of downloading them (recognised by SHA-256, then by name). Missing files are downloaded.",
        };

        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command Build()
        {
            var command = new Command("fetch", "Store a ReqIF version's metamodel, schemas and specification PDF from omg.org or a local directory.");
            command.Add(GlobalOptions.RepositoryRoot);
            command.Add(GlobalOptions.Version);
            command.Add(NoSpecsOption);
            command.Add(NoDefaultOption);
            command.Add(FromOption);

            command.SetAction(async (parseResult, cancellationToken) =>
            {
                if (!GlobalOptions.TryResolveVersion(parseResult.GetValue(GlobalOptions.Version)!, out var descriptor))
                {
                    return 2;
                }

                var layout = GlobalOptions.LayoutFrom(parseResult);
                var includeSpecs = !parseResult.GetValue(NoSpecsOption);
                var from = parseResult.GetValue(FromOption);

                using var httpClient = new HttpClient();
                var service = new KnowledgeGenerationService(new SourceFetcher(httpClient), new PythonSpecExtractRunner(new ProcessRunner(), new UvProvisioner(httpClient)));

                AnsiConsole.MarkupLineInterpolated($"Fetching ReqIF {descriptor.Version}{(includeSpecs ? " (including the specification PDF)" : string.Empty)}{(from is null ? string.Empty : $" from {from}")}...");

                try
                {
                    var entries = await service.FetchAsync(layout, descriptor, includeSpecs, from is null ? null : Path.GetFullPath(from), !parseResult.GetValue(NoDefaultOption), cancellationToken);
                    foreach (var entry in entries)
                    {
                        var note = entry.MatchesPublished ? string.Empty : " [yellow](differs from the published file)[/]";
                        AnsiConsole.MarkupLine($"  [green]{entry.Origin}[/] {Markup.Escape(entry.File)} (sha256 {entry.Sha256[..12]}...){note}");
                    }
                }
                catch (Exception exception) when (exception is HttpRequestException or InvalidDataException or IOException)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Fetch failed:[/] {exception.Message}");
                    return 1;
                }

                AnsiConsole.MarkupLineInterpolated($"[green]Done.[/] Run 'reqifsharp-sage generate --version {descriptor.Version}' next.");
                return 0;
            });

            return command;
        }
    }
}
