// -------------------------------------------------------------------------------------------------
// <copyright file="GlobalOptions.cs" company="Starion Group S.A.">
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
    using System.Linq;
    using System.Text.Encodings.Web;
    using System.Text.Json;

    using ReqIFSharp.Sage.Knowledge;

    using Spectre.Console;

    /// <summary>
    /// Options and helpers shared by more than one verb.
    /// </summary>
    public static class GlobalOptions
    {
        /// <summary>
        /// The plugin repository root; see <see cref="RepositoryRootResolver"/> for the default resolution order.
        /// </summary>
        public static readonly Option<string?> RepositoryRoot = new("--repository-root")
        {
            Description = "The reqifsharp-sage plugin root. Defaults to $CLAUDE_PLUGIN_ROOT, then the directory above the CLI's own tools folder, then the current directory.",
        };

        /// <summary>
        /// The ReqIF version to operate on.
        /// </summary>
        public static readonly Option<string> Version = new("--version")
        {
            Description = "The ReqIF version to operate on.",
            DefaultValueFactory = _ => KnownReqIFVersions.Current.Version,
        };

        /// <summary>
        /// Requests machine-readable JSON output.
        /// </summary>
        public static readonly Option<bool> Json = new("--json")
        {
            Description = "Emit machine-readable JSON instead of human-readable text.",
        };

        /// <summary>
        /// The JSON options used for every <c>--json</c> output.
        /// </summary>
        public static readonly JsonSerializerOptions JsonOutputOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>
        /// Resolves the repository layout from the parsed <see cref="RepositoryRoot"/> option.
        /// </summary>
        /// <param name="parseResult">The parse result.</param>
        /// <returns>The layout.</returns>
        public static RepositoryLayout LayoutFrom(ParseResult parseResult)
        {
            return new RepositoryLayout(RepositoryRootResolver.Resolve(parseResult.GetValue(RepositoryRoot)));
        }

        /// <summary>
        /// Looks up a ReqIF version, printing the known versions when it is unknown.
        /// </summary>
        /// <param name="version">The requested version.</param>
        /// <param name="descriptor">The descriptor, when known.</param>
        /// <returns>Whether the version is known.</returns>
        public static bool TryResolveVersion(string version, out ReqIFVersionDescriptor descriptor)
        {
            if (KnownReqIFVersions.TryFind(version, out var found))
            {
                descriptor = found!;
                return true;
            }

            AnsiConsole.MarkupLineInterpolated($"[red]Unknown ReqIF version '{version}'.[/] Known versions: {string.Join(", ", KnownReqIFVersions.All.Select(v => v.Version))}");
            descriptor = null!;
            return false;
        }

        /// <summary>
        /// Writes <paramref name="value"/> as JSON to standard output.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value.</param>
        public static void WriteJson<T>(T value)
        {
            Console.Out.WriteLine(JsonSerializer.Serialize(value, JsonOutputOptions));
        }
    }
}
