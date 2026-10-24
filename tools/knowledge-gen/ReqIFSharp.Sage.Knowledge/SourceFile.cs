// -------------------------------------------------------------------------------------------------
// <copyright file="SourceFile.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
