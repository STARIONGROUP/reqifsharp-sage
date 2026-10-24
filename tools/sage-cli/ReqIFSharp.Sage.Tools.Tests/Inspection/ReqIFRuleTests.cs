// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFRuleTests.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Tests.Inspection
{
    using System.Linq;

    using ReqIFSharp.Sage.Tools.Inspection;

    /// <summary>
    /// Each ReqIF 1.2 rule (R-xxx) and each Implementation Guide rule (G-xxx) is reported for the element of a toy
    /// fixture that violates it, with the catalog's severity and category.
    /// </summary>
    [TestFixture]
    public class ReqIFRuleTests
    {
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-001", "dt-dup")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-002", null)]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-003", "req-no-last-change")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-004", "req-unknown-type")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-005", "rel-dangling")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-006", "req-wrong-kind")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-007", "sh-no-object")]
        [TestCase(InspectionFixtures.Values, "R-008", "req-more-values")]
        [TestCase(InspectionFixtures.Values, "R-008", "ad-wrong-datatype")]
        [TestCase(InspectionFixtures.Values, "R-009", "req-values")]
        [TestCase(InspectionFixtures.Values, "R-010", "req-values")]
        [TestCase(InspectionFixtures.Values, "R-011", "req-values")]
        [TestCase(InspectionFixtures.Values, "R-011", "ad-default-elsewhere")]
        [TestCase(InspectionFixtures.Values, "R-012", "req-values")]
        [TestCase(InspectionFixtures.Values, "R-013", "req-more-values")]
        [TestCase(InspectionFixtures.Values, "R-014", "req-values")]
        [TestCase(InspectionFixtures.Values, "R-015", "req-more-values")]
        [TestCase(InspectionFixtures.Values, "R-016", "req-values")]
        [TestCase(InspectionFixtures.Values, "R-017", "dt-inverted")]
        [TestCase(InspectionFixtures.Values, "R-018", "ad-default-kind")]
        [TestCase(InspectionFixtures.Values, "R-019", "ad-default-elsewhere")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-020", "ev-nameless")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-020", "group-nameless")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-021", "group-nameless")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-022", "sh-a-again")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-023", "req-orphan")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-024", "header-toaster")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-025", "header-toaster")]
        [TestCase(InspectionFixtures.IdentifiersAndReferences, "R-026", "header-toaster")]
        [TestCase(InspectionFixtures.Xhtml, "R-027", "req-text-direct")]
        [TestCase(InspectionFixtures.Xhtml, "R-028", "req-script")]
        [TestCase(InspectionFixtures.Xhtml, "R-029", "req-class")]
        [TestCase(InspectionFixtures.Xhtml, "R-030", "req-object-without-data")]
        [TestCase(InspectionFixtures.Xhtml, "R-031", "req-pdf-without-alternative")]
        [TestCase(InspectionFixtures.Xhtml, "R-031", "req-jpeg-alternative")]
        [TestCase(InspectionFixtures.Xhtml, "R-032", "req-extra-object-attribute")]
        [TestCase(InspectionFixtures.Xhtml, "R-033", "req-pdf-without-alternative")]
        [TestCase(InspectionFixtures.Xhtml, "R-034", "req-simplified")]
        [TestCase(InspectionFixtures.Xhtml, "G-008", "req-object-without-type")]
        [TestCase(InspectionFixtures.Xhtml, "G-009", "req-formatted-alternative-text")]
        [TestCase(InspectionFixtures.Xhtml, "G-010", "req-heading")]
        [TestCase(InspectionFixtures.Xhtml, "G-011", "req-margin")]
        [TestCase(InspectionFixtures.Guide, "G-002", null)]
        [TestCase(InspectionFixtures.Guide, "G-003", "ad-unknown-system-name")]
        [TestCase(InspectionFixtures.Guide, "G-004", "ad-case-variant")]
        [TestCase(InspectionFixtures.Guide, "G-005", "ad-chapter-name-as-string")]
        [TestCase(InspectionFixtures.Guide, "G-006", "ad-specification-chapter-name")]
        [TestCase(InspectionFixtures.Guide, "G-007", "ad-foreign-deleted")]
        [TestCase(InspectionFixtures.Guide, "G-013", "rel-across-specifications")]
        [TestCase(InspectionFixtures.Guide, "G-014", "type-group-with-attributes")]
        [TestCase(InspectionFixtures.Guide, "G-015", "sh-brakes-heading")]
        [TestCase(InspectionFixtures.Guide, "G-016", "dt-status")]
        [TestCase(InspectionFixtures.Guide, "G-017", null)]
        public void Rule_is_reported_for_the_offending_element(string fixture, string ruleId, string? identifier)
        {
            var report = InspectionFixtures.Inspect(fixture);
            var rule = InspectionRuleCatalog.Get(ruleId);

            var matches = report.Findings.Where(finding => finding.RuleId == ruleId && finding.Identifier == identifier).ToList();

            Assert.That(matches, Is.Not.Empty, InspectionFixtures.Describe(report.Findings));

            Assert.Multiple(() =>
            {
                Assert.That(matches.Select(finding => finding.Severity), Has.All.EqualTo(rule.Severity));
                Assert.That(matches.Select(finding => finding.Category), Has.All.EqualTo(rule.Category));
                Assert.That(matches.Select(finding => finding.Document), Has.All.EqualTo(fixture));
                Assert.That(matches.Select(finding => finding.Line), Has.All.Not.Null);
                Assert.That(matches.Select(finding => finding.Reference), Has.All.StartWith(rule.IsGuideRule ? "PSI Implementation Guide 1.7 §" : "ReqIF 1.2 §"));
                Assert.That(matches.Select(finding => finding.Message), Has.All.Not.Empty);
            });
        }

        [Test]
        public void A_well_formed_object_with_its_image_present_yields_no_finding()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.Xhtml);

            Assert.That(report.Findings.Where(finding => finding.Identifier == "req-ok-object"), Is.Empty);
        }

        [Test]
        public void Guide_rules_are_not_reported_unless_requested()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.Guide, InspectionFixtures.WithoutGuide);

            Assert.That(report.Findings, Is.Empty, InspectionFixtures.Describe(report.Findings));
        }

        [Test]
        public void The_guide_fixture_is_valid_ReqIF_so_it_only_yields_guide_findings()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.Guide);

            Assert.That(report.Findings.Select(finding => finding.Category), Has.All.EqualTo("implementation-guide"), InspectionFixtures.Describe(report.Findings));
        }

        [Test]
        public void The_value_rules_still_run_when_reqifsharp_cannot_deserialize_the_document()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.Values);

            Assert.Multiple(() =>
            {
                Assert.That(report.Findings, Has.Some.Matches<InspectionFinding>(finding => finding != null && finding.RuleId == "LOAD" && finding.Message.Contains("reqifsharp cannot deserialize")));
                Assert.That(report.Findings, Has.Some.Matches<InspectionFinding>(finding => finding != null && finding.RuleId == "R-011"));
            });
        }

        [Test]
        public void A_global_reference_to_an_object_of_another_document_of_the_archive_is_resolved()
        {
            var directory = TemporaryDirectory.Create();
            var first = InspectionFixtures.Utf8(InspectionFixtures.Text(InspectionFixtures.Clean));
            var second = InspectionFixtures.Utf8(InspectionFixtures.CleanWithForeignRelationTarget());

            var archive = System.IO.Path.Combine(directory, "pair.reqifz");
            InspectionFixtures.WriteArchive(archive, ("first.reqif", first), ("second.reqif", second));

            var alone = System.IO.Path.Combine(directory, "second.reqif");
            System.IO.File.WriteAllBytes(alone, second);

            var inspector = new ReqIFInspector();
            var archiveReport = inspector.Inspect(archive, InspectionFixtures.WithoutGuide);
            var aloneReport = inspector.Inspect(alone, InspectionFixtures.WithoutGuide);

            Assert.Multiple(() =>
            {
                Assert.That(archiveReport.Findings.Where(finding => finding.RuleId == "R-005"), Is.Empty, InspectionFixtures.Describe(archiveReport.Findings));
                Assert.That(aloneReport.Findings, Has.Some.Matches<InspectionFinding>(finding => finding != null && finding.RuleId == "R-005" && finding.Identifier == "rel-grind-refines-brew"));
            });
        }
    }
}
