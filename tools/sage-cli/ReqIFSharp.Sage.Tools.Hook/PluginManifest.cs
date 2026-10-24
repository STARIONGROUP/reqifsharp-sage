// -------------------------------------------------------------------------------------------------
// <copyright file="PluginManifest.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Hook
{
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// The subset of <c>.claude-plugin/plugin.json</c> this hook reads.
    /// </summary>
    public sealed class PluginManifest
    {
        [JsonPropertyName("sageCliVersion")]
        public string? SageCliVersion { get; set; }

        /// <summary>
        /// Reads <c>&lt;pluginRoot&gt;/.claude-plugin/plugin.json</c> and returns its
        /// <c>sageCliVersion</c> field, or <c>null</c> if the file is missing or unreadable.
        /// </summary>
        public static string? ReadPinnedCliVersion(string pluginRoot)
        {
            try
            {
                var path = Path.Combine(pluginRoot, ".claude-plugin", "plugin.json");
                if (!File.Exists(path))
                {
                    return null;
                }

                var json = File.ReadAllText(path);
                var manifest = JsonSerializer.Deserialize(json, HookJsonContext.Default.PluginManifest);
                return manifest?.SageCliVersion;
            }
            catch
            {
                return null;
            }
        }
    }
}
