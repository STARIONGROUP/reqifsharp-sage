// -------------------------------------------------------------------------------------------------
// <copyright file="GitHubRelease.cs" company="Starion Group S.A.">
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
    using System.Text.Json.Serialization;

    /// <summary>
    /// The subset of the GitHub Releases API's response this hook needs.
    /// </summary>
    public sealed class GitHubRelease
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset> Assets { get; set; } = [];
    }

    /// <summary>
    /// One downloadable asset of a <see cref="GitHubRelease"/>.
    /// </summary>
    public sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// A minimal, source-generated <see cref="System.Text.Json.Serialization.JsonSerializerContext"/> so
    /// this NativeAOT-published hook never falls back to reflection-based JSON (de)serialization.
    /// </summary>
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(GitHubRelease))]
    [JsonSerializable(typeof(HookInput))]
    [JsonSerializable(typeof(HookOutput))]
    [JsonSerializable(typeof(CheckResult))]
    [JsonSerializable(typeof(PluginManifest))]
    public partial class HookJsonContext : JsonSerializerContext
    {
    }
}
