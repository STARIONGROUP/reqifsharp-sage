// -------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Starion Group S.A.">
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

using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ReqIFSharp.Sage.Tools.Hook;

// This SessionStart hook must never block session startup and never print anything but a valid
// hookSpecificOutput envelope (or nothing) to stdout - any failure anywhere below is swallowed and
// the hook exits 0 silently, deferring to the next session. See CLAUDE.md's "small hook, big CLI" split.
if (args.Length == 3 && args[0] == BackgroundProvisioning.ProvisionArgument)
{
    // Detached provisioning mode, started by a SessionStart run that found no cached CLI: download it
    // under a generous timeout, outside any session's start-up budget.
    await ProvisionAsync(args[1], args[2]);
    return 0;
}

try
{
    using var overallTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
    await RunAsync(overallTimeout.Token);
}
catch
{
    // Never surface a failure to the user - silence is the correct degraded behavior here.
}

return 0;

static async Task ProvisionAsync(string pluginRoot, string cliVersion)
{
    var runtimeIdentifier = RuntimeIdentifier.Current();
    if (runtimeIdentifier is null)
    {
        return;
    }

    var cliDirectory = CacheLayout.CliDirectoryFor(pluginRoot, cliVersion, runtimeIdentifier);
    try
    {
        using var timeout = new CancellationTokenSource(BackgroundProvisioning.Timeout);
        using var httpClient = new HttpClient { Timeout = BackgroundProvisioning.Timeout };
        await new CliProvisioner(httpClient).EnsureAsync(pluginRoot, cliVersion, timeout.Token);
    }
    catch
    {
        // Nothing to report from a detached process; the next session tries again.
    }
    finally
    {
        BackgroundProvisioning.ReleaseLock(cliDirectory);
    }
}

static async Task RunAsync(CancellationToken cancellationToken)
{
    var stdin = await Console.In.ReadToEndAsync(cancellationToken);
    HookInput? input = null;
    try
    {
        input = JsonSerializer.Deserialize(stdin, HookJsonContext.Default.HookInput);
    }
    catch (JsonException)
    {
        // Malformed/absent stdin - proceed as a generic session start.
    }

    var pluginRoot = ResolvePluginRoot();
    if (pluginRoot is null)
    {
        return;
    }

    // A CLI built from source in the plugin checkout wins (development, or before the first tools-v*
    // release exists).
    var runtimeIdentifier = RuntimeIdentifier.Current();
    var cliPath = runtimeIdentifier is null ? null : LocalCliLocator.Find(pluginRoot, RuntimeIdentifier.ExecutableName(runtimeIdentifier));

    // Otherwise use the released CLI pinned in plugin.json - but only from the cache: downloading it
    // does not fit the session's start-up budget, so a missing CLI is fetched by a detached process.
    if (cliPath is null)
    {
        var cliVersion = PluginManifest.ReadPinnedCliVersion(pluginRoot);
        if (cliVersion is null || runtimeIdentifier is null)
        {
            return;
        }

        cliPath = CliProvisioner.TryGetCached(pluginRoot, cliVersion);
        if (cliPath is null)
        {
            if (BackgroundProvisioning.TryStart(pluginRoot, cliVersion, runtimeIdentifier))
            {
                WriteContext(CheckResultSummarizer.Provisioning(pluginRoot));
            }

            return;
        }
    }

    var checkResult = await new CheckRunner().RunAsync(cliPath, pluginRoot, cancellationToken);
    var additionalContext = CheckResultSummarizer.Summarize(checkResult, cliPath.Contains(' ') ? $"\"{cliPath}\"" : cliPath, pluginRoot);
    if (additionalContext is null)
    {
        return;
    }

    WriteContext(additionalContext);
    _ = input; // input.source could steer behavior in the future (e.g. skip on "compact"); unused today.
}

static void WriteContext(string additionalContext)
{
    var output = new HookOutput
    {
        HookSpecificOutput = new HookSpecificOutput
        {
            HookEventName = "SessionStart",
            AdditionalContext = additionalContext,
        },
    };

    Console.WriteLine(JsonSerializer.Serialize(output, HookJsonContext.Default.HookOutput));
}

static string? ResolvePluginRoot()
{
    var fromEnvironment = Environment.GetEnvironmentVariable("CLAUDE_PLUGIN_ROOT");
    if (!string.IsNullOrWhiteSpace(fromEnvironment) && Directory.Exists(fromEnvironment))
    {
        return Path.GetFullPath(fromEnvironment);
    }

    // Fallback: this executable lives at <pluginRoot>/hooks/native/<rid>/reqifsharp-sage-hook[.exe].
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    return directory.Parent?.Parent?.Parent?.FullName;
}
