// -------------------------------------------------------------------------------------------------
// <copyright file="GitHubRelease.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
