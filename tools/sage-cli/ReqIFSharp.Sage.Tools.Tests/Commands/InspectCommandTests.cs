// -------------------------------------------------------------------------------------------------
// <copyright file="InspectCommandTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Tests.Commands
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    using ReqIFSharp.Sage.Knowledge;
    using ReqIFSharp.Sage.Tools.Commands;
    using ReqIFSharp.Sage.Tools.Inspection;

    [TestFixture]
    public class InspectCommandTests
    {
        private string repositoryRoot = null!;

        private static string Clean => Fixture("clean.reqif");

        private static string WithErrors => Fixture("identifiers-and-references.reqif");

        [SetUp]
        public void SetUp()
        {
            this.repositoryRoot = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(this.repositoryRoot);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(this.repositoryRoot, recursive: true);
        }

        [Test]
        public void Inspect_returns_0_for_a_clean_file_and_1_for_a_file_with_errors()
        {
            Assert.That(this.Run(Clean).ExitCode, Is.EqualTo(0));
            Assert.That(this.Run(WithErrors).ExitCode, Is.EqualTo(1));
        }

        [Test]
        public void Inspect_returns_2_without_a_file_or_for_a_missing_file()
        {
            Assert.That(ConsoleCapture.Run(InspectCommand.Build(), "--repository-root", this.repositoryRoot).ExitCode, Is.EqualTo(2));
            Assert.That(this.Run(Path.Combine(this.repositoryRoot, "missing.reqif")).ExitCode, Is.EqualTo(2));
        }

        [TestCase(false, null, false)]
        [TestCase(true, null, true)]
        [TestCase(true, "--no-guide-rules", false)]
        [TestCase(false, "--guide-rules", true)]
        public void Inspect_applies_guide_rules_only_when_a_guide_is_imported_or_forced(bool guideImported, string? flag, bool expected)
        {
            if (guideImported)
            {
                new InstalledVersionsStore(new RepositoryLayout(this.repositoryRoot).KnowledgeRoot).MarkGuideImported("1.7");
            }

            var arguments = flag is null ? new[] { "--json" } : new[] { "--json", flag };
            using var report = JsonDocument.Parse(this.Run(Clean, arguments).Output);

            Assert.That(report.RootElement.GetProperty("guideRulesApplied").GetBoolean(), Is.EqualTo(expected));
        }

        [Test]
        public void Inspect_json_has_the_report_document_and_finding_fields_the_skill_documents()
        {
            using var report = JsonDocument.Parse(this.Run(WithErrors, "--json").Output);
            var root = report.RootElement;

            Assert.That(Names(root), Is.EquivalentTo(new[] { "file", "guideRulesApplied", "errorCount", "warningCount", "infoCount", "documents", "findings" }));
            Assert.That(root.GetProperty("errorCount").GetInt32(), Is.GreaterThan(0));
            Assert.That(
                Names(root.GetProperty("documents")[0]),
                Is.SupersetOf(new[] { "document", "identifier", "title", "reqIFVersion", "sourceToolId", "reqIFToolId", "specObjectCount", "specificationCount", "specRelationCount", "datatypeCount", "specTypeCount" }));
            Assert.That(
                Names(root.GetProperty("findings")[0]),
                Is.EquivalentTo(new[] { "severity", "ruleId", "category", "identifier", "message", "line", "reference", "document" }));
        }

        [Test]
        public void Inspect_rules_json_lists_every_catalog_rule()
        {
            var (exitCode, output) = ConsoleCapture.Run(InspectCommand.Build(), "--rules", "--json");
            using var rules = JsonDocument.Parse(output);

            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(rules.RootElement.GetArrayLength(), Is.EqualTo(InspectionRuleCatalog.All.Count));
            Assert.That(Names(rules.RootElement[0]), Is.EquivalentTo(new[] { "id", "severity", "category", "reference", "description", "isGuideRule" }));
        }

        private static string Fixture(string name) => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Inspection", name);

        private static IEnumerable<string> Names(JsonElement element) => element.EnumerateObject().Select(p => p.Name).ToList();

        private (int ExitCode, string Output) Run(string file, params string[] extra)
        {
            return ConsoleCapture.Run(InspectCommand.Build(), new[] { file, "--repository-root", this.repositoryRoot }.Concat(extra).ToArray());
        }
    }
}
