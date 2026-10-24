// -------------------------------------------------------------------------------------------------
// <copyright file="PluginManifest.cs" company="Starion Group S.A.">
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
