// -------------------------------------------------------------------------------------------------
// <copyright file="FetchManifestEntry.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
