// -------------------------------------------------------------------------------------------------
// <copyright file="ImportGuideCommand.cs" company="Starion Group S.A.">
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

    using ReqIFSharp.Sage.Knowledge;

    using Spectre.Console;

    /// <summary>
    /// <c>reqifsharp-sage import-guide</c>: stores the user's copy of the prostep ivip ReqIF
    /// Implementation Guide. The guide is not public, so it is never downloaded or committed.
    /// </summary>
    public static class ImportGuideCommand
    {
        private static readonly Argument<string> PdfArgument = new("pdf")
        {
            Description = "Path to your copy of the prostep ivip ReqIF Implementation Guide PDF.",
        };

        private static readonly Option<string?> GuideVersionOption = new("--version")
        {
            Description = "The guide version; only needed when the file is not a known release.",
        };

        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command Build()
        {
            var command = new Command("import-guide", "Import your copy of the prostep ivip ReqIF Implementation Guide (never downloaded, never committed).");
            command.Add(PdfArgument);
            command.Add(GuideVersionOption);
            command.Add(GlobalOptions.RepositoryRoot);

            command.SetAction(parseResult =>
            {
                var layout = GlobalOptions.LayoutFrom(parseResult);
                var pdf = Path.GetFullPath(parseResult.GetValue(PdfArgument)!);

                try
                {
                    var result = GuideImporter.Import(layout, pdf, parseResult.GetValue(GuideVersionOption));
                    if (result.Warning is not null)
                    {
                        AnsiConsole.MarkupLineInterpolated($"[yellow]warning[/] {result.Warning}");
                    }

                    AnsiConsole.MarkupLineInterpolated($"[green]Imported[/] implementation guide {result.Version} to {result.StoredPath}.");
                    AnsiConsole.MarkupLine("Run 'reqifsharp-sage generate' to extract its text. It stays on this machine: the sources/ and knowledge/ folders are git-ignored.");
                    return 0;
                }
                catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]Import failed:[/] {exception.Message}");
                    return 1;
                }
            });

            return command;
        }
    }
}
