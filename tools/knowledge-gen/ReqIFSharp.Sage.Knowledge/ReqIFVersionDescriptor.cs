// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFVersionDescriptor.cs" company="Starion Group S.A.">
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
    using System.Linq;

    /// <summary>
    /// Everything needed to fetch one published version of the OMG ReqIF specification.
    /// </summary>
    /// <param name="Version">The ReqIF version, e.g. <c>1.2</c>.</param>
    /// <param name="IsCurrent">Whether this is the version used when none is given.</param>
    /// <param name="OmgDocumentId">The OMG document number of the formal specification, e.g. <c>formal/16-07-01</c>.</param>
    /// <param name="Files">The source files.</param>
    public sealed record ReqIFVersionDescriptor(string Version, bool IsCurrent, string OmgDocumentId, IReadOnlyList<SourceFile> Files)
    {
        /// <summary>
        /// Gets the file with the given role.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <returns>The file.</returns>
        public SourceFile FileFor(SourceFileRole role) => this.Files.First(f => f.Role == role);
    }
}
