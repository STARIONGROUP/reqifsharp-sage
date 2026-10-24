// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionOptions.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    /// <summary>
    /// Options for <see cref="ReqIFInspector.Inspect"/>.
    /// </summary>
    /// <param name="IncludeGuideRules">Whether to also run the prostep ivip ReqIF Implementation Guide rules (G-xxx).</param>
    /// <param name="GuideVersion">
    /// The Implementation Guide version to check against; <c>null</c> means the latest implemented version
    /// (<see cref="InspectionRuleCatalog.GuideVersion"/>). Any other version is reported with rule G-000 and the
    /// implemented version is applied.
    /// </param>
    public sealed record InspectionOptions(bool IncludeGuideRules, string? GuideVersion);
}
