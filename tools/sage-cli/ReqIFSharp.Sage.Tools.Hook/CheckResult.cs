// -------------------------------------------------------------------------------------------------
// <copyright file="CheckResult.cs" company="Starion Group S.A.">
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

    /// <summary>
    /// Mirrors the parts of <c>reqifsharp-sage check --json</c> the hook needs. Defined here rather than
    /// by referencing the CLI so the hook stays dependency-free and NativeAOT-publishable.
    /// </summary>
    public sealed class CheckResult
    {
        /// <summary>
        /// Gets or sets the default ReqIF version.
        /// </summary>
        public string? Default { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether any ReqIF version is recorded.
        /// </summary>
        public bool HasAnyVersion { get; set; }

        /// <summary>
        /// Gets or sets the ReqIF versions.
        /// </summary>
        public List<CheckResultVersion> Versions { get; set; } = [];

        /// <summary>
        /// Gets or sets the implementation guides.
        /// </summary>
        public List<CheckResultGuide> Guides { get; set; } = [];

        /// <summary>
        /// Gets or sets the generated reqifsharp API reference, if any.
        /// </summary>
        public CheckResultApi? Api { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the API reference matches the CLI's bundled ReqIFSharp.
        /// </summary>
        public bool ApiUpToDate { get; set; }

        /// <summary>
        /// Gets or sets the ReqIFSharp version the CLI bundles.
        /// </summary>
        public string? BundledReqIFSharpVersion { get; set; }
    }

    /// <summary>
    /// One entry of <see cref="CheckResult.Versions"/>.
    /// </summary>
    public sealed class CheckResultVersion
    {
        /// <summary>
        /// Gets or sets the ReqIF version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether its sources are stored.
        /// </summary>
        public bool Fetched { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether its knowledge is generated.
        /// </summary>
        public bool Generated { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether its specification text is extracted.
        /// </summary>
        public bool SpecGenerated { get; set; }
    }

    /// <summary>
    /// One entry of <see cref="CheckResult.Guides"/>.
    /// </summary>
    public sealed class CheckResultGuide
    {
        /// <summary>
        /// Gets or sets the guide version.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the user's PDF is imported.
        /// </summary>
        public bool Imported { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether its text is extracted.
        /// </summary>
        public bool Generated { get; set; }
    }

    /// <summary>
    /// The <see cref="CheckResult.Api"/> entry.
    /// </summary>
    public sealed class CheckResultApi
    {
        /// <summary>
        /// Gets or sets the ReqIFSharp version the reference describes.
        /// </summary>
        public string ReqIFSharpVersion { get; set; } = string.Empty;
    }
}
