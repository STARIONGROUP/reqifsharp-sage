// -------------------------------------------------------------------------------------------------
// <copyright file="RemoveCommand.cs" company="Starion Group S.A.">
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
    using System.Linq;

    using ReqIFSharp.Sage.Knowledge;

    using Spectre.Console;

    /// <summary>
    /// <c>reqifsharp-sage remove</c>: deletes a ReqIF version's sources and knowledge, or (with
    /// <c>--guide</c>) an imported implementation guide and its extracted text.
    /// </summary>
    public static class RemoveCommand
    {
        private static readonly Option<bool> ForceOption = new("--force")
        {
            Description = "Allow removing the default version or the only installed version.",
        };

        private static readonly Option<string?> GuideOption = new("--guide")
        {
            Description = "Remove this imported implementation guide version (and its extracted text) instead of a ReqIF version.",
        };

        /// <summary>
        /// Builds the command.
        /// </summary>
        /// <returns>The command.</returns>
        public static Command Build()
        {
            var command = new Command("remove", "Delete a ReqIF version's sources and knowledge, or an imported implementation guide (--guide).");
            command.Add(GlobalOptions.RepositoryRoot);
            command.Add(GlobalOptions.Version);
            command.Add(GuideOption);
            command.Add(ForceOption);

            command.SetAction(parseResult =>
            {
                var layout = GlobalOptions.LayoutFrom(parseResult);
                var store = new InstalledVersionsStore(layout.KnowledgeRoot);
                var guide = parseResult.GetValue(GuideOption);

                if (guide is not null)
                {
                    // Only a guide recorded in installed.json can be removed: the value becomes part of a
                    // directory path that is deleted recursively.
                    if (store.Load().Guides.All(g => g.Version != guide))
                    {
                        AnsiConsole.MarkupLineInterpolated($"[yellow]Implementation guide '{guide}' is not imported.[/]");
                        return 1;
                    }

                    DeleteIfPresent(layout.GuideSourcesDirectoryFor(guide));
                    DeleteIfPresent(layout.GuideKnowledgeDirectoryFor(guide));
                    store.RemoveGuide(guide);
                    AnsiConsole.MarkupLineInterpolated($"[green]Removed implementation guide {guide}.[/]");
                    return 0;
                }

                var version = parseResult.GetValue(GlobalOptions.Version)!;
                var force = parseResult.GetValue(ForceOption);
                var manifest = store.Load();

                if (manifest.Versions.All(v => v.Version != version))
                {
                    AnsiConsole.MarkupLineInterpolated($"[yellow]ReqIF {version} is not installed.[/]");
                    return 1;
                }

                if (!force && manifest.Default == version && manifest.Versions.Count > 1)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ReqIF {version} is the default.[/] Run 'use' to pick another default first, or pass --force.");
                    return 1;
                }

                if (!force && manifest.Versions.Count == 1)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]ReqIF {version} is the only installed version.[/] Pass --force to remove it anyway.");
                    return 1;
                }

                DeleteIfPresent(layout.SourcesDirectoryFor(version));
                DeleteIfPresent(layout.KnowledgeDirectoryFor(version));
                store.Remove(version);

                AnsiConsole.MarkupLineInterpolated($"[green]Removed ReqIF {version}.[/] Imported guides and the reqifsharp API reference are left in place.");
                return 0;
            });

            return command;
        }

        private static void DeleteIfPresent(string directory)
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
