// -------------------------------------------------------------------------------------------------
// <copyright file="KnownGuideVersions.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The hand-maintained registry of prostep ivip ReqIF Implementation Guide versions.
    /// </summary>
    public static class KnownGuideVersions
    {
        /// <summary>
        /// Every known guide version.
        /// </summary>
        public static readonly IReadOnlyList<GuideDescriptor> All =
        [
            new GuideDescriptor(
                Version: "1.7",
                Title: "prostep ivip ReqIF Implementation Guide",
                ReleaseDate: "2019-12-16",
                ReferredReqIFVersion: "1.2",
                Sha256: "1e01cf2ac07dcfd71a54f06d9ce7713d1aaa05236a915a8c54e40f2beeb7a64d"),
        ];

        /// <summary>
        /// Gets the most recent known version.
        /// </summary>
        public static GuideDescriptor Current => All[^1];

        /// <summary>
        /// Finds a known guide by version.
        /// </summary>
        /// <param name="version">The version, e.g. <c>1.7</c>.</param>
        /// <returns>The descriptor, or null.</returns>
        public static GuideDescriptor? Find(string version) => All.FirstOrDefault(g => g.Version == version);

        /// <summary>
        /// Finds a known guide by the SHA-256 of its PDF.
        /// </summary>
        /// <param name="sha256">The lower-case hex SHA-256.</param>
        /// <returns>The descriptor, or null when the file is not a known release.</returns>
        public static GuideDescriptor? FindBySha256(string sha256) => All.FirstOrDefault(g => string.Equals(g.Sha256, sha256, StringComparison.OrdinalIgnoreCase));
    }
}
