// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionReport.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The full result of inspecting one <c>.reqif</c> file or <c>.reqifz</c> archive.
    /// </summary>
    /// <param name="File">The inspected path, as passed to <see cref="ReqIFInspector.Inspect"/>.</param>
    /// <param name="Documents">One entry per ReqIF document found (one for a <c>.reqif</c> file, one per <c>.reqif</c> entry of an archive).</param>
    /// <param name="Findings">All findings, ordered by document, line, rule id and identifier.</param>
    public sealed record InspectionReport(string File, IReadOnlyList<InspectedDocument> Documents, IReadOnlyList<InspectionFinding> Findings)
    {
        /// <summary>
        /// Gets the number of findings with severity "error".
        /// </summary>
        public int ErrorCount => this.Findings.Count(finding => finding.Severity == InspectionSeverity.Error);

        /// <summary>
        /// Gets the number of findings with severity "warning".
        /// </summary>
        public int WarningCount => this.Findings.Count(finding => finding.Severity == InspectionSeverity.Warning);

        /// <summary>
        /// Gets the number of findings with severity "info".
        /// </summary>
        public int InfoCount => this.Findings.Count(finding => finding.Severity == InspectionSeverity.Info);
    }

    /// <summary>
    /// A summary of one inspected ReqIF document.
    /// </summary>
    /// <param name="Identifier">The IDENTIFIER of the REQ-IF-HEADER.</param>
    /// <param name="Title">The header TITLE.</param>
    /// <param name="ReqIFVersion">The header REQ-IF-VERSION.</param>
    /// <param name="SourceToolId">The header SOURCE-TOOL-ID.</param>
    /// <param name="ReqIFToolId">The header REQ-IF-TOOL-ID.</param>
    /// <param name="SpecObjectCount">The number of SPEC-OBJECT elements.</param>
    /// <param name="SpecificationCount">The number of SPECIFICATION elements.</param>
    /// <param name="SpecRelationCount">The number of SPEC-RELATION elements.</param>
    /// <param name="DatatypeCount">The number of DATATYPE-DEFINITION-* elements.</param>
    /// <param name="SpecTypeCount">The number of spec types (SPEC-OBJECT-TYPE, SPECIFICATION-TYPE, SPEC-RELATION-TYPE, RELATION-GROUP-TYPE).</param>
    /// <param name="Document">The file name, or the archive entry name for a document in a <c>.reqifz</c> archive.</param>
    public sealed record InspectedDocument(
        string? Identifier,
        string? Title,
        string? ReqIFVersion,
        string? SourceToolId,
        string? ReqIFToolId,
        int SpecObjectCount,
        int SpecificationCount,
        int SpecRelationCount,
        int DatatypeCount,
        int SpecTypeCount,
        string? Document = null);
}
