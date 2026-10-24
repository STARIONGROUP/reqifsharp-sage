// -------------------------------------------------------------------------------------------------
// <copyright file="DocumentSource.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    using System;

    /// <summary>
    /// The raw content of one ReqIF document to inspect: a <c>.reqif</c> file, or one <c>.reqif</c> entry of a <c>.reqifz</c> archive.
    /// </summary>
    /// <param name="Name">The file name or the archive entry name; used as <see cref="InspectionFinding.Document"/>.</param>
    /// <param name="Content">The bytes of the document.</param>
    /// <param name="AttachmentExists">
    /// Tells whether a path relative to the document (an XHTML object data URL, already unescaped) names an existing
    /// file: next to the <c>.reqif</c> file, or in the archive.
    /// </param>
    /// <param name="AttachmentLocation">Where <paramref name="AttachmentExists"/> looks, for messages: "the .reqifz archive" or "the folder of the .reqif file".</param>
    internal sealed record DocumentSource(string Name, byte[] Content, Func<string, bool> AttachmentExists, string AttachmentLocation);
}
