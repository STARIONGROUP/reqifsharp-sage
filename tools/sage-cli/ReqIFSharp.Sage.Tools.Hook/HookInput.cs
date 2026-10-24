// -------------------------------------------------------------------------------------------------
// <copyright file="HookInput.cs" company="Starion Group S.A.">
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
    using System.Text.Json.Serialization;

    /// <summary>
    /// The JSON Claude Code sends to a <c>SessionStart</c> hook's stdin.
    /// </summary>
    public sealed class HookInput
    {
        [JsonPropertyName("session_id")]
        public string? SessionId { get; set; }

        [JsonPropertyName("cwd")]
        public string? Cwd { get; set; }

        [JsonPropertyName("hook_event_name")]
        public string? HookEventName { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }
    }

    /// <summary>
    /// The <c>hookSpecificOutput</c> envelope Claude Code expects on stdout to add additional context.
    /// </summary>
    public sealed class HookOutput
    {
        [JsonPropertyName("hookSpecificOutput")]
        public HookSpecificOutput HookSpecificOutput { get; set; } = new();
    }

    /// <summary>
    /// See <see cref="HookOutput.HookSpecificOutput"/>.
    /// </summary>
    public sealed class HookSpecificOutput
    {
        [JsonPropertyName("hookEventName")]
        public string HookEventName { get; set; } = "SessionStart";

        [JsonPropertyName("additionalContext")]
        public string AdditionalContext { get; set; } = string.Empty;
    }
}
