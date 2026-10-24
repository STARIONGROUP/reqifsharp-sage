// -------------------------------------------------------------------------------------------------
// <copyright file="FetchManifestEntry.cs" company="Starion Group S.A.">
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
    using System;

    /// <summary>
    /// One row of <c>sources/&lt;version&gt;/fetch-manifest.json</c>: where a source file came from.
    /// </summary>
    /// <param name="Url">The published URL of the file.</param>
    /// <param name="File">The path under <c>sources/&lt;version&gt;/</c>.</param>
    /// <param name="Sha256">The SHA-256 of the stored file.</param>
    /// <param name="MatchesPublished">Whether <paramref name="Sha256"/> equals the hash registered for the published file.</param>
    /// <param name="Origin"><c>download</c>, or <c>local</c> when copied from a <c>fetch --from</c> directory.</param>
    /// <param name="FetchedAt">When the file was stored.</param>
    public sealed record FetchManifestEntry(string Url, string File, string Sha256, bool MatchesPublished, string Origin, DateTimeOffset FetchedAt);
}
