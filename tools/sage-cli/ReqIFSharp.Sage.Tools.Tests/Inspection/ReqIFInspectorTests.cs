// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFInspectorTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Tests.Inspection
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using ReqIFSharp.Sage.Tools.Inspection;

    /// <summary>
    /// The inspection engine: loading, schema validation, archives, robustness and the shape of the report.
    /// </summary>
    [TestFixture]
    public class ReqIFInspectorTests
    {
        [Test]
        public void A_clean_document_yields_no_finding_at_all_even_with_guide_rules()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.Clean);

            Assert.That(report.Findings, Is.Empty, InspectionFixtures.Describe(report.Findings));
        }

        [Test]
        public void A_clean_document_is_summarised_from_its_header_and_content()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.Clean);

            Assert.That(report.Documents, Has.Count.EqualTo(1));

            var document = report.Documents[0];

            Assert.Multiple(() =>
            {
                Assert.That(report.File, Is.EqualTo(InspectionFixtures.PathOf(InspectionFixtures.Clean)));
                Assert.That(document.Identifier, Is.EqualTo("header-coffee"));
                Assert.That(document.Title, Is.EqualTo("Coffee machine requirements"));
                Assert.That(document.ReqIFVersion, Is.EqualTo("1.0"));
                Assert.That(document.SourceToolId, Is.EqualTo("toy authoring tool 1.0"));
                Assert.That(document.ReqIFToolId, Is.EqualTo("toy exporter 1.0"));
                Assert.That(document.SpecObjectCount, Is.EqualTo(2));
                Assert.That(document.SpecificationCount, Is.EqualTo(1));
                Assert.That(document.SpecRelationCount, Is.EqualTo(1));
                Assert.That(document.DatatypeCount, Is.EqualTo(7));
                Assert.That(document.SpecTypeCount, Is.EqualTo(3));
                Assert.That(document.Document, Is.EqualTo(InspectionFixtures.Clean));
                Assert.That(report.ErrorCount + report.WarningCount + report.InfoCount, Is.Zero);
            });
        }

        [Test]
        public void A_missing_file_yields_a_single_load_error_instead_of_an_exception()
        {
            var path = Path.Combine(TemporaryDirectory.Create(), "absent.reqif");

            var report = new ReqIFInspector().Inspect(path, InspectionFixtures.WithoutGuide);

            Assert.Multiple(() =>
            {
                Assert.That(report.Findings, Has.Count.EqualTo(1));
                Assert.That(report.Findings[0].RuleId, Is.EqualTo("LOAD"));
                Assert.That(report.Findings[0].Severity, Is.EqualTo("error"));
                Assert.That(report.Documents, Is.Empty);
                Assert.That(report.ErrorCount, Is.EqualTo(1));
            });
        }

        [Test]
        public void A_directory_yields_a_load_error()
        {
            var report = new ReqIFInspector().Inspect(TemporaryDirectory.Create(), InspectionFixtures.WithoutGuide);

            Assert.That(report.Findings.Single().RuleId, Is.EqualTo("LOAD"));
        }

        [Test]
        public void A_document_that_is_not_well_formed_yields_only_a_load_error_with_its_line()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.Malformed, InspectionFixtures.WithoutGuide);

            Assert.Multiple(() =>
            {
                Assert.That(report.Findings, Has.Count.EqualTo(1), InspectionFixtures.Describe(report.Findings));
                Assert.That(report.Findings[0].RuleId, Is.EqualTo("LOAD"));
                Assert.That(report.Findings[0].Line, Is.GreaterThan(1));
                Assert.That(report.Findings[0].Document, Is.EqualTo(InspectionFixtures.Malformed));
                Assert.That(report.Documents.Single().SpecObjectCount, Is.Zero);
            });
        }

        [TestCase("")]
        [TestCase("<?xml version=\"1.0\" encoding=\"UTF-8\"?><NOT-REQIF/>")]
        [TestCase("<?xml version=\"1.0\" encoding=\"UTF-8\"?><REQ-IF xmlns=\"urn:other\"/>")]
        public void Content_that_is_not_a_ReqIF_document_yields_a_load_error(string content)
        {
            var path = Path.Combine(TemporaryDirectory.Create(), "odd.reqif");
            File.WriteAllText(path, content);

            var report = new ReqIFInspector().Inspect(path, InspectionFixtures.WithoutGuide);

            Assert.That(report.Findings.Select(finding => finding.RuleId), Is.EqualTo(new[] { "LOAD" }), InspectionFixtures.Describe(report.Findings));
        }

        [Test]
        public void A_root_without_namespace_is_a_load_warning_and_skips_schema_validation()
        {
            var path = Path.Combine(TemporaryDirectory.Create(), "no-namespace.reqif");
            File.WriteAllText(path, InspectionFixtures.Text(InspectionFixtures.Clean).Replace(" xmlns=\"http://www.omg.org/spec/ReqIF/20110401/reqif.xsd\"", string.Empty, StringComparison.Ordinal));

            var report = new ReqIFInspector().Inspect(path, InspectionFixtures.WithoutGuide);

            Assert.Multiple(() =>
            {
                Assert.That(report.Findings.Select(finding => (finding.RuleId, finding.Severity)), Is.EqualTo(new[] { ("LOAD", "warning") }), InspectionFixtures.Describe(report.Findings));
                Assert.That(report.Documents.Single().SpecObjectCount, Is.EqualTo(2));
            });
        }

        [Test]
        public void Schema_violations_are_reported_as_xsd_findings_with_line_and_owner()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.IdentifiersAndReferences, InspectionFixtures.WithoutGuide);

            var xsd = report.Findings.Where(finding => finding.RuleId == "XSD").ToList();

            Assert.Multiple(() =>
            {
                Assert.That(xsd, Is.Not.Empty);
                Assert.That(xsd.Select(finding => finding.Category), Has.All.EqualTo("schema"));
                Assert.That(xsd.Select(finding => finding.Line), Has.All.Not.Null);
                Assert.That(xsd, Has.Some.Matches<InspectionFinding>(finding => finding != null && finding.Message.Contains("type-missing") && finding.Severity == "error"));
            });
        }

        [Test]
        public void An_element_reqifsharp_does_not_support_is_reported_as_a_reader_finding()
        {
            var path = Path.Combine(TemporaryDirectory.Create(), "extra.reqif");
            File.WriteAllText(path, InspectionFixtures.CleanWithUnsupportedElement());

            var report = new ReqIFInspector().Inspect(path, InspectionFixtures.WithoutGuide);

            Assert.That(report.Findings, Has.Some.Matches<InspectionFinding>(finding => finding != null && finding.RuleId == "READER" && finding.Message.Contains("FANCY-EXTRA")), InspectionFixtures.Describe(report.Findings));
        }

        [Test]
        public void Findings_are_ordered_by_document_line_rule_and_identifier()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.IdentifiersAndReferences);

            var expected = report.Findings
                .OrderBy(finding => finding.Document, StringComparer.Ordinal)
                .ThenBy(finding => finding.Line ?? 0)
                .ThenBy(finding => finding.RuleId, StringComparer.Ordinal)
                .ThenBy(finding => finding.Identifier, StringComparer.Ordinal)
                .ThenBy(finding => finding.Message, StringComparer.Ordinal)
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(report.Findings, Is.EqualTo(expected));
                Assert.That(InspectionFixtures.Inspect(InspectionFixtures.IdentifiersAndReferences).Findings, Is.EqualTo(report.Findings));
            });
        }

        [Test]
        public void An_archive_is_inspected_entry_by_entry_and_findings_name_their_entry()
        {
            var directory = TemporaryDirectory.Create();
            var first = InspectionFixtures.WithConversation(
                InspectionFixtures.CleanWithBrewText("Coffee <xhtml:object data=\"files/cup.png\" type=\"image/png\">cup</xhtml:object> and <xhtml:object data=\"files/saucer.png\" type=\"image/png\">saucer</xhtml:object>"),
                "conversation-1");
            var second = InspectionFixtures.WithConversation(InspectionFixtures.Text(InspectionFixtures.Clean), "conversation-2");
            var archive = Path.Combine(directory, "pair.reqifz");

            InspectionFixtures.WriteArchive(
                archive,
                ("docs/first.reqif", InspectionFixtures.Utf8(first)),
                ("docs/files/cup.png", File.ReadAllBytes(InspectionFixtures.PathOf("xhtml-files/present.png"))),
                ("second.reqif", InspectionFixtures.Utf8(second)));

            var report = new ReqIFInspector().Inspect(archive, InspectionFixtures.WithGuide);

            Assert.Multiple(() =>
            {
                Assert.That(report.Documents.Select(document => document.Document), Is.EqualTo(new[] { "docs/first.reqif", "second.reqif" }));

                var missing = report.Findings.Where(finding => finding.RuleId == "R-033").ToList();
                Assert.That(missing, Has.Count.EqualTo(1), InspectionFixtures.Describe(report.Findings));
                Assert.That(missing.Single().Document, Is.EqualTo("docs/first.reqif"));
                Assert.That(missing.Single().Message, Does.Contain("files/saucer.png"));

                Assert.That(report.Findings, Has.Some.Matches<InspectionFinding>(finding => finding != null && finding.RuleId == "G-012" && finding.Document == null));
                Assert.That(report.Findings.Where(finding => finding.Severity != "info" && finding.RuleId is not "R-033" and not "G-012"), Is.Empty, InspectionFixtures.Describe(report.Findings));
            });
        }

        [Test]
        public void An_archive_without_a_reqif_entry_yields_a_load_error()
        {
            var archive = Path.Combine(TemporaryDirectory.Create(), "empty.reqifz");
            InspectionFixtures.WriteArchive(archive, ("readme.txt", InspectionFixtures.Utf8("no ReqIF here")));

            var report = new ReqIFInspector().Inspect(archive, InspectionFixtures.WithoutGuide);

            Assert.That(report.Findings.Single().RuleId, Is.EqualTo("LOAD"));
        }

        [Test]
        public void A_corrupt_archive_yields_a_load_error()
        {
            var archive = Path.Combine(TemporaryDirectory.Create(), "corrupt.reqifz");
            File.WriteAllBytes(archive, [0x50, 0x4B, 0x03, 0x04, 0x00, 0x01, 0x02]);

            var report = new ReqIFInspector().Inspect(archive, InspectionFixtures.WithoutGuide);

            Assert.That(report.Findings.Select(finding => finding.RuleId), Has.Some.EqualTo("LOAD").Or.Some.EqualTo("INSPECT"));
            Assert.That(report.ErrorCount, Is.GreaterThan(0));
        }

        [Test]
        public void A_file_extension_other_than_reqif_is_reported_when_guide_rules_are_on()
        {
            var path = Path.Combine(TemporaryDirectory.Create(), "coffee.xml");
            File.Copy(InspectionFixtures.PathOf(InspectionFixtures.Clean), path);

            var withGuide = new ReqIFInspector().Inspect(path, InspectionFixtures.WithGuide);
            var withoutGuide = new ReqIFInspector().Inspect(path, InspectionFixtures.WithoutGuide);

            Assert.Multiple(() =>
            {
                Assert.That(withGuide.Findings.Single().RuleId, Is.EqualTo("G-001"));
                Assert.That(withGuide.Findings.Single().Document, Is.Null);
                Assert.That(withoutGuide.Findings, Is.Empty);
            });
        }

        [Test]
        public void An_unknown_guide_version_is_reported_and_the_implemented_version_applied()
        {
            var report = InspectionFixtures.Inspect(InspectionFixtures.Guide, new InspectionOptions(IncludeGuideRules: true, GuideVersion: "1.6"));

            Assert.Multiple(() =>
            {
                Assert.That(report.Findings, Has.Some.Matches<InspectionFinding>(finding => finding != null && finding.RuleId == "G-000" && finding.Message.Contains("1.6")));
                Assert.That(report.Findings, Has.Some.Matches<InspectionFinding>(finding => finding != null && finding.RuleId == "G-003"));
            });
        }

        [Test]
        public void Every_rule_of_the_catalog_is_exercised_by_the_fixtures()
        {
            var reported = new HashSet<string>(StringComparer.Ordinal);

            foreach (var fixture in new[] { InspectionFixtures.IdentifiersAndReferences, InspectionFixtures.Values, InspectionFixtures.Xhtml, InspectionFixtures.Guide })
            {
                reported.UnionWith(InspectionFixtures.Inspect(fixture, new InspectionOptions(true, "0.9")).Findings.Select(finding => finding.RuleId));
            }

            var directory = TemporaryDirectory.Create();
            var renamed = Path.Combine(directory, "renamed.xml");
            File.Copy(InspectionFixtures.PathOf(InspectionFixtures.Clean), renamed);
            reported.UnionWith(new ReqIFInspector().Inspect(renamed, InspectionFixtures.WithGuide).Findings.Select(finding => finding.RuleId));

            var archive = Path.Combine(directory, "conversations.reqifz");
            InspectionFixtures.WriteArchive(
                archive,
                ("a.reqif", InspectionFixtures.Utf8(InspectionFixtures.WithConversation(InspectionFixtures.Text(InspectionFixtures.Clean), "one"))),
                ("b.reqif", InspectionFixtures.Utf8(InspectionFixtures.WithConversation(InspectionFixtures.Text(InspectionFixtures.Clean), "two"))));
            reported.UnionWith(new ReqIFInspector().Inspect(archive, InspectionFixtures.WithGuide).Findings.Select(finding => finding.RuleId));

            var extra = Path.Combine(directory, "extra.reqif");
            File.WriteAllText(extra, InspectionFixtures.CleanWithUnsupportedElement());
            reported.UnionWith(new ReqIFInspector().Inspect(extra, InspectionFixtures.WithoutGuide).Findings.Select(finding => finding.RuleId));

            reported.UnionWith(InspectionFixtures.Inspect(InspectionFixtures.Malformed).Findings.Select(finding => finding.RuleId));

            var expected = InspectionRuleCatalog.All.Select(rule => rule.Id).Where(id => id != "INSPECT");

            Assert.That(reported, Is.SupersetOf(expected));
        }
    }
}
