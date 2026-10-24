// -------------------------------------------------------------------------------------------------
// <copyright file="SourceFile.cs" company="Starion Group S.A.">
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
    /// One OMG source file of a ReqIF version: where it is published, where it is stored under
    /// <c>sources/&lt;version&gt;/</c>, and the SHA-256 of the published file.
    /// </summary>
    /// <param name="Role">What the file is used for.</param>
    /// <param name="Url">The omg.org download URL.</param>
    /// <param name="RelativePath">The path under <c>sources/&lt;version&gt;/</c>, using forward slashes.</param>
    /// <param name="Sha256">The lower-case hex SHA-256 of the published file, used to recognise local copies under any name.</param>
    public sealed record SourceFile(SourceFileRole Role, string Url, string RelativePath, string Sha256)
    {
        /// <summary>
        /// Gets the file name part of <see cref="RelativePath"/>.
        /// </summary>
        public string FileName => this.RelativePath[(this.RelativePath.LastIndexOf('/') + 1)..];

        /// <summary>
        /// Gets a value indicating whether the file is a specification document (skipped by <c>fetch --no-specs</c>).
        /// </summary>
        public bool IsSpecificationDocument => this.Role == SourceFileRole.SpecificationPdf;
    }
}
