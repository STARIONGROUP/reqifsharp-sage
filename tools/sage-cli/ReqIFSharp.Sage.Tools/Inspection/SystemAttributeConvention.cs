// -------------------------------------------------------------------------------------------------
// <copyright file="SystemAttributeConvention.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    /// <summary>
    /// One system attribute naming convention of the prostep ivip ReqIF Implementation Guide (§2.3): an attribute
    /// definition LONG-NAME, the kind of element whose SpecType defines it, and the recommended attribute definition kind.
    /// </summary>
    /// <param name="Name">The case-sensitive attribute definition LONG-NAME, e.g. "ReqIF.Text".</param>
    /// <param name="ElementKind">"SpecObject", "Specification" or "SpecRelation".</param>
    /// <param name="DefinitionKind">
    /// The recommended attribute definition kind, as the suffix of the XML element name: "STRING", "XHTML", "DATE",
    /// "BOOLEAN" or "ENUMERATION".
    /// </param>
    public sealed record SystemAttributeConvention(string Name, string ElementKind, string DefinitionKind);
}
