// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionFinding.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    /// <summary>
    /// One finding produced by <see cref="ReqIFInspector"/>.
    /// </summary>
    /// <param name="Severity">"error", "warning" or "info".</param>
    /// <param name="RuleId">
    /// The id of the rule that produced the finding: "LOAD", "XSD", "READER", "INSPECT", "R-001".. for ReqIF 1.2 rules
    /// or "G-000".. for prostep ivip Implementation Guide rules. <see cref="InspectionRuleCatalog"/> lists them all.
    /// </param>
    /// <param name="Category">A short kebab-case category, e.g. "schema", "unresolved-reference" or "implementation-guide".</param>
    /// <param name="Identifier">The IDENTIFIER of the element concerned (or of its nearest identifiable owner), when known.</param>
    /// <param name="Message">A self-explanatory, human-readable description.</param>
    /// <param name="Line">The 1-based line in the inspected document where the offending element starts, when known.</param>
    /// <param name="Reference">The clause the rule is based on, e.g. "ReqIF 1.2 §10.8.21" or "PSI Implementation Guide 1.7 §2.3".</param>
    /// <param name="Document">
    /// The inspected document the finding is about: the file name of a <c>.reqif</c> file, or the entry name of a
    /// <c>.reqif</c> file in a <c>.reqifz</c> archive; <c>null</c> when the finding is about the file or archive as a whole.
    /// </param>
    public sealed record InspectionFinding(
        string Severity,
        string RuleId,
        string Category,
        string? Identifier,
        string Message,
        int? Line,
        string? Reference,
        string? Document = null);
}
