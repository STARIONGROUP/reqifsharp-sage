// -------------------------------------------------------------------------------------------------
// <copyright file="GuideDescriptor.cs" company="Starion Group S.A.">
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
