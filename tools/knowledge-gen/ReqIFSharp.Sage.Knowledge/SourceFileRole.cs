// -------------------------------------------------------------------------------------------------
// <copyright file="SourceFileRole.cs" company="Starion Group S.A.">
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
    /// What a <see cref="SourceFile"/> is used for.
    /// </summary>
    public enum SourceFileRole
    {
        /// <summary>
        /// The formal specification PDF (input of the <c>spec/</c> clause extraction).
        /// </summary>
        SpecificationPdf,

        /// <summary>
        /// The CMOF metamodel (input of <c>metamodel/</c>).
        /// </summary>
        Metamodel,

        /// <summary>
        /// The ReqIF XML schema <c>reqif.xsd</c> (input of <c>schema/</c>).
        /// </summary>
        Schema,

        /// <summary>
        /// The XHTML driver schema <c>driver.xsd</c> (input of <c>schema/xhtml-subset.md</c>).
        /// </summary>
        XhtmlDriverSchema,
    }
}
