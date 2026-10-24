// -------------------------------------------------------------------------------------------------
// <copyright file="InstalledVersionsManifest.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Knowledge
{
    using System.Collections.Generic;

    /// <summary>
    /// The state of one ReqIF version on this machine.
    /// </summary>
    /// <param name="Version">The ReqIF version.</param>
    /// <param name="Fetched">Whether its sources are stored.</param>
    /// <param name="Generated">Whether its metamodel (and schema) knowledge is generated.</param>
    /// <param name="SpecGenerated">Whether its specification clause text is extracted.</param>
    public sealed record InstalledVersion(string Version, bool Fetched, bool Generated, bool SpecGenerated);

    /// <summary>
    /// The state of one imported prostep implementation guide version on this machine.
    /// </summary>
    /// <param name="Version">The guide version.</param>
    /// <param name="Imported">Whether the user's PDF is stored.</param>
    /// <param name="Generated">Whether its clause text is extracted.</param>
    public sealed record InstalledGuide(string Version, bool Imported, bool Generated);

    /// <summary>
    /// The generated reqifsharp API reference.
    /// </summary>
    /// <param name="ReqIFSharpVersion">The ReqIFSharp package version it describes.</param>
    /// <param name="ExtensionsVersion">The ReqIFSharp.Extensions package version it describes.</param>
    public sealed record InstalledApi(string ReqIFSharpVersion, string ExtensionsVersion);

    /// <summary>
    /// The git-ignored, local-only <c>knowledge/installed.json</c>: what has been fetched, imported and
    /// generated on this machine, and which ReqIF version is the default.
    /// </summary>
    /// <param name="Default">The default ReqIF version, if any.</param>
    /// <param name="Versions">The ReqIF versions.</param>
    /// <param name="Guides">The implementation guide versions.</param>
    /// <param name="Api">The generated reqifsharp API reference, if any.</param>
    public sealed record InstalledVersionsManifest(string? Default, List<InstalledVersion> Versions, List<InstalledGuide> Guides, InstalledApi? Api)
    {
        /// <summary>
        /// Gets an empty manifest, used when no <c>installed.json</c> exists yet.
        /// </summary>
        public static InstalledVersionsManifest Empty => new(null, [], [], null);
    }
}
