// -------------------------------------------------------------------------------------------------
// <copyright file="SourceFileRole.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
