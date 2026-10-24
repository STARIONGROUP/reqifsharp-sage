// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionRule.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    /// <summary>
    /// One rule <see cref="ReqIFInspector"/> can report, as listed by <see cref="InspectionRuleCatalog"/>.
    /// </summary>
    /// <param name="Id">The rule id, e.g. "R-001" or "G-003".</param>
    /// <param name="Severity">The default severity of the rule's findings ("error", "warning" or "info").</param>
    /// <param name="Category">The short kebab-case category of the rule's findings.</param>
    /// <param name="Reference">The clause the rule is based on, when there is one.</param>
    /// <param name="Description">A one-line description of what the rule checks.</param>
    /// <param name="IsGuideRule">Whether the rule only runs when <see cref="InspectionOptions.IncludeGuideRules"/> is set.</param>
    public sealed record InspectionRule(string Id, string Severity, string Category, string? Reference, string Description, bool IsGuideRule = false);
}
