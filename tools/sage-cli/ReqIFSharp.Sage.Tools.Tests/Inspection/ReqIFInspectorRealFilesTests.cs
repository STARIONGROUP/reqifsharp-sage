// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFInspectorRealFilesTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Tests.Inspection
{
    using System;
    using System.IO;
    using System.Linq;

    using ReqIFSharp.Sage.Tools.Inspection;

    /// <summary>
    /// Runs the inspector over the sample files of a local reqifsharp checkout (not part of this repository). Set
    /// REQIFSHARP_TESTDATA to its <c>ReqIFSharp.Tests/TestData</c> folder, or keep the checkout next to this one.
    /// </summary>
    [TestFixture]
    [Explicit("Needs a local reqifsharp checkout; prints a summary per file.")]
    public class ReqIFInspectorRealFilesTests
    {
        [Test]
        public void Inspect_never_throws_on_the_reqifsharp_sample_files_and_prints_their_findings()
        {
            var directory = Environment.GetEnvironmentVariable("REQIFSHARP_TESTDATA") ?? FindSiblingTestData();

            if (directory is null || !Directory.Exists(directory))
            {
                Assert.Ignore("reqifsharp TestData folder not found.");
                return;
            }

            var inspector = new ReqIFInspector();

            foreach (var file in Directory.GetFiles(directory).OrderBy(file => file, StringComparer.Ordinal))
            {
                var report = inspector.Inspect(file, new InspectionOptions(IncludeGuideRules: true, GuideVersion: null));

                TestContext.Out.WriteLine($"{Path.GetFileName(file)}: {report.Documents.Count} document(s), {report.ErrorCount} error(s), {report.WarningCount} warning(s), {report.InfoCount} info");

                foreach (var group in report.Findings.GroupBy(finding => (finding.Severity, finding.RuleId)).OrderBy(group => group.Key.RuleId, StringComparer.Ordinal))
                {
                    TestContext.Out.WriteLine($"    {group.Key.RuleId} {group.Key.Severity} x{group.Count()}: {group.First().Message}");
                }

                Assert.That(report, Is.Not.Null);
            }
        }

        private static string? FindSiblingTestData()
        {
            for (var current = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); current is not null; current = current.Parent)
            {
                var candidate = Path.Combine(current.FullName, "reqifsharp", "ReqIFSharp.Tests", "TestData");

                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
