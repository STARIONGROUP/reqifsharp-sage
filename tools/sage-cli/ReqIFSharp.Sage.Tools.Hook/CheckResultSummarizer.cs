// -------------------------------------------------------------------------------------------------
// <copyright file="CheckResultSummarizer.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Hook
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Turns a <see cref="CheckResult"/> into the one-paragraph <c>additionalContext</c> the hook gives
    /// Claude at session start: how to invoke the CLI (it is not on PATH) and what still needs setting up.
    /// </summary>
    public static class CheckResultSummarizer
    {
        /// <summary>
        /// Builds the context text for a session in which the released CLI is still being downloaded in
        /// the background.
        /// </summary>
        /// <param name="pluginRoot">The plugin root directory.</param>
        /// <returns>The context text.</returns>
        public static string Provisioning(string pluginRoot)
        {
            return $"reqifsharp-sage: the plugin root is {pluginRoot}. Its CLI is being downloaded in the background " +
                   "(first session after installing or updating the plugin) and will be available next session. Until then " +
                   $"it can be run with `dotnet run --project \"{pluginRoot}/tools/sage-cli/ReqIFSharp.Sage.Tools\" -- <verb> --repository-root \"{pluginRoot}\"` " +
                   "if the .NET 10 SDK is installed (see the knowledge-setup skill); skills that only read the knowledge base work without it.";
        }

        /// <summary>
        /// Builds the context text.
        /// </summary>
        /// <param name="result">The check result, or null when <c>check</c> could not be run.</param>
        /// <param name="cliInvocation">The command that runs the CLI (an absolute path, quoted when needed).</param>
        /// <param name="pluginRoot">The plugin root directory.</param>
        /// <returns>The context text, or null when there is nothing useful to say.</returns>
        public static string? Summarize(CheckResult? result, string? cliInvocation, string pluginRoot)
        {
            if (cliInvocation is null)
            {
                return null;
            }

            var parts = new List<string>
            {
                $"reqifsharp-sage: the plugin root is {pluginRoot}; run its CLI as {cliInvocation} (it is not on PATH) and pass --repository-root \"{pluginRoot}\".",
            };

            if (result is null)
            {
                parts.Add("Its status check failed; run the CLI's 'check' verb to see why.");
                return string.Join(" ", parts);
            }

            var defaultEntry = result.Versions.FirstOrDefault(v => v.Version == result.Default);
            if (!result.HasAnyVersion || defaultEntry is null)
            {
                parts.Add("No ReqIF knowledge base is set up yet. Before answering ReqIF questions from the knowledge base, offer the knowledge-setup skill (fetch then generate).");
            }
            else if (!defaultEntry.Generated)
            {
                parts.Add($"ReqIF {defaultEntry.Version} is fetched but not generated; offer to run 'generate'.");
            }
            else if (!defaultEntry.SpecGenerated)
            {
                parts.Add($"ReqIF {defaultEntry.Version} metamodel and schema knowledge is ready; verbatim spec citation is unavailable (PDF not fetched or Python missing), so cite clause numbers only.");
            }
            else
            {
                parts.Add($"ReqIF {defaultEntry.Version} knowledge is ready.");
            }

            var guide = result.Guides.Where(g => g.Imported).OrderBy(g => g.Version, System.StringComparer.Ordinal).LastOrDefault();
            if (guide is null)
            {
                parts.Add("The prostep ivip Implementation Guide is not imported (optional; it is not public, the user must supply the PDF via 'import-guide').");
            }
            else if (!guide.Generated)
            {
                parts.Add($"Implementation guide {guide.Version} is imported but not extracted; offer to run 'generate'.");
            }

            if (result.Api is null)
            {
                parts.Add("The reqifsharp API reference is not generated yet ('generate' builds it).");
            }
            else if (!result.ApiUpToDate)
            {
                parts.Add($"The reqifsharp API reference describes ReqIFSharp {result.Api.ReqIFSharpVersion} but the CLI bundles {result.BundledReqIFSharpVersion}; offer to run 'generate'.");
            }

            return string.Join(" ", parts);
        }
    }
}
