// -------------------------------------------------------------------------------------------------
// <copyright file="UseCommand.cs" company="Starion Group S.A.">
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

    using Spectre.Console;

    using ReqIFSharp.Sage.Knowledge;

    /// <summary>
    /// <c>reqifsharp-sage use --version &lt;version&gt;</c>: switches the default version, refusing if it
    /// hasn't been generated yet. Exists so a future multi-version world (2.6, 3.0 alongside 2.5.1) has a
    /// way to switch without re-fetching.
    /// </summary>
    public static class UseCommand
    {
        /// <summary>
        /// Builds the command.
        /// </summary>
        public static Command Build()
        {
            var command = new Command("use", "Set the default ReqIF version.");
            command.Add(GlobalOptions.RepositoryRoot);
            command.Add(GlobalOptions.Version);

            command.SetAction(parseResult =>
            {
                var version = parseResult.GetValue(GlobalOptions.Version)!;
                var layout = new RepositoryLayout(RepositoryRootResolver.Resolve(parseResult.GetValue(GlobalOptions.RepositoryRoot)));

                try
                {
                    new InstalledVersionsStore(layout.KnowledgeRoot).SetDefault(version);
                }
                catch (InvalidOperationException exception)
                {
                    AnsiConsole.MarkupLineInterpolated($"[red]{exception.Message}[/]");
                    return 1;
                }

                AnsiConsole.MarkupLineInterpolated($"[green]ReqIF {version} is now the default.[/]");
                return 0;
            });

            return command;
        }
    }
}
