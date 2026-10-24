// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionContext.cs" company="Starion Group S.A.">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Collects the findings of one inspection; severity, category and default reference come from <see cref="InspectionRuleCatalog"/>.
    /// </summary>
    internal sealed class InspectionContext
    {
        private readonly List<InspectionFinding> findings = [];

        /// <summary>
        /// Gets or sets the name of the document the next findings are about (<c>null</c> for the file as a whole).
        /// </summary>
        internal string? Document { get; set; }

        /// <summary>
        /// Gets a value indicating whether the guide rules run.
        /// </summary>
        internal bool IncludeGuideRules { get; init; }

        /// <summary>
        /// Reports a finding.
        /// </summary>
        /// <param name="ruleId">The rule id.</param>
        /// <param name="message">The self-explanatory message.</param>
        /// <param name="element">The offending element; supplies the identifier and line unless given explicitly.</param>
        /// <param name="identifier">The identifier, when not taken from <paramref name="element"/>.</param>
        /// <param name="line">The line, when not taken from <paramref name="element"/>.</param>
        /// <param name="reference">A more specific reference than the rule's default.</param>
        /// <param name="severity">A severity overriding the rule's default.</param>
        internal void Report(string ruleId, string message, XElement? element = null, string? identifier = null, int? line = null, string? reference = null, string? severity = null)
        {
            var rule = InspectionRuleCatalog.Get(ruleId);

            if (rule.IsGuideRule && !this.IncludeGuideRules)
            {
                return;
            }

            identifier ??= element is null ? null : ReqIFXml.OwnerIdentifier(element);
            line ??= element is null ? null : ReqIFXml.Line(element);

            this.findings.Add(new InspectionFinding(severity ?? rule.Severity, rule.Id, rule.Category, identifier, message, line, reference ?? rule.Reference, this.Document));
        }

        /// <summary>
        /// Gets the distinct findings in their deterministic order: by document, line, rule id, identifier and message.
        /// </summary>
        /// <returns>The ordered findings.</returns>
        internal IReadOnlyList<InspectionFinding> OrderedFindings() =>
            this.findings
                .Distinct()
                .OrderBy(finding => finding.Document is null ? 0 : 1)
                .ThenBy(finding => finding.Document, StringComparer.Ordinal)
                .ThenBy(finding => finding.Line ?? 0)
                .ThenBy(finding => finding.RuleId, StringComparer.Ordinal)
                .ThenBy(finding => finding.Identifier, StringComparer.Ordinal)
                .ThenBy(finding => finding.Message, StringComparer.Ordinal)
                .ToList();
    }
}
