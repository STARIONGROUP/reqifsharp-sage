// -------------------------------------------------------------------------------------------------
// <copyright file="DocumentSource.cs" company="Starion Group S.A.">
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
