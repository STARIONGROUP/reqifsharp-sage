// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionRule.cs" company="Starion Group S.A.">
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
