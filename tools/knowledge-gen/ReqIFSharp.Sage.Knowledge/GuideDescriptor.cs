// -------------------------------------------------------------------------------------------------
// <copyright file="GuideDescriptor.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    /// <summary>
    /// A known version of the prostep ivip ReqIF Implementation Guide. The guide is not publicly
    /// available: it is never downloaded or committed, only imported from a copy the user supplies.
    /// </summary>
    /// <param name="Version">The guide version, e.g. <c>1.7</c>.</param>
    /// <param name="Title">The document title.</param>
    /// <param name="ReleaseDate">The release date as printed on the cover (ISO 8601).</param>
    /// <param name="ReferredReqIFVersion">The ReqIF version the guide refers to.</param>
    /// <param name="Sha256">The SHA-256 of the released PDF, used to recognise an imported copy.</param>
    public sealed record GuideDescriptor(string Version, string Title, string ReleaseDate, string ReferredReqIFVersion, string Sha256)
    {
        /// <summary>
        /// Gets the file name the imported PDF is stored under in <c>sources/psi-guide/&lt;version&gt;/</c>.
        /// </summary>
        public string FileName => $"PSI-ReqIF-Implementation-Guide-{this.Version}.pdf";
    }
}
