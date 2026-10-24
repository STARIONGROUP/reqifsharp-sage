// -------------------------------------------------------------------------------------------------
// <copyright file="CheckRunner.cs" company="Starion Group S.A.">
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
    using System.Diagnostics;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Runs the provisioned CLI's offline <c>check --json</c> verb and parses its result.
    /// </summary>
    public sealed class CheckRunner
    {
        /// <summary>
        /// Runs <c>&lt;cliExecutablePath&gt; check --json --repository-root &lt;pluginRoot&gt;</c>.
        /// Never throws: any failure (the process can't start, times out, exits non-zero, or emits
        /// unparseable output) results in a <c>null</c> return.
        /// </summary>
        public async Task<CheckResult?> RunAsync(string cliExecutablePath, string pluginRoot, CancellationToken cancellationToken)
        {
            try
            {
                var startInfo = new ProcessStartInfo(cliExecutablePath)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                startInfo.ArgumentList.Add("check");
                startInfo.ArgumentList.Add("--json");
                startInfo.ArgumentList.Add("--repository-root");
                startInfo.ArgumentList.Add(pluginRoot);

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    return null;
                }

                var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode != 0)
                {
                    return null;
                }

                var output = await outputTask;
                return JsonSerializer.Deserialize(output, HookJsonContext.Default.CheckResult);
            }
            catch
            {
                return null;
            }
        }
    }
}
