// -------------------------------------------------------------------------------------------------
// <copyright file="CheckCommandTests.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Tests.Commands
{
    using System.IO;
    using System.Linq;

    using ReqIFSharp.Sage.Knowledge;
    using ReqIFSharp.Sage.Tools.Commands;

    [TestFixture]
    public class CheckCommandTests
    {
        private string repositoryRoot = null!;

        [SetUp]
        public void SetUp()
        {
            this.repositoryRoot = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(this.repositoryRoot))
            {
                Directory.Delete(this.repositoryRoot, recursive: true);
            }
        }

        [Test]
        public void Invoke_never_makes_a_network_call_and_returns_0_when_nothing_is_fetched()
        {
            var exitCode = CheckCommand.Build().Parse(["--repository-root", this.repositoryRoot]).Invoke();

            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        public void Invoke_returns_0_once_a_version_is_fully_generated()
        {
            var layout = new RepositoryLayout(this.repositoryRoot);
            var store = new InstalledVersionsStore(layout.KnowledgeRoot);
            store.MarkFetched("1.2", setAsDefault: true);
            store.MarkGenerated("1.2", specGenerated: true);

            var exitCode = CheckCommand.Build().Parse(["--repository-root", this.repositoryRoot]).Invoke();

            Assert.That(exitCode, Is.EqualTo(0));
        }

        [Test]
        public void BuildReport_with_nothing_installed_says_what_to_fetch()
        {
            var report = CheckCommand.BuildReport(new RepositoryLayout(this.repositoryRoot));

            Assert.That(report.HasAnyVersion, Is.False);
            Assert.That(report.Summary[0], Does.Contain("fetch --version 1.2"));
            Assert.That(report.Summary, Has.Some.Contains("import-guide"));
            Assert.That(report.ApiUpToDate, Is.False);
        }

        [Test]
        public void BuildReport_reports_guides_and_whether_the_api_reference_matches_the_bundled_package()
        {
            var layout = new RepositoryLayout(this.repositoryRoot);
            var store = new InstalledVersionsStore(layout.KnowledgeRoot);
            store.MarkFetched("1.2", setAsDefault: true);
            store.MarkGenerated("1.2", specGenerated: true);
            store.MarkGuideImported("1.7");
            store.MarkGuideGenerated("1.7", succeeded: true);
            store.MarkApiGenerated(new InstalledApi(BundledPackages.ReqIFSharpVersion, BundledPackages.ExtensionsVersion));

            var report = CheckCommand.BuildReport(layout);

            Assert.That(report.Default, Is.EqualTo("1.2"));
            Assert.That(report.Guides.Single().Generated, Is.True);
            Assert.That(report.ApiUpToDate, Is.True);
            Assert.That(report.RepositoryRoot, Is.EqualTo(layout.RepositoryRoot));
            Assert.That(report.Summary, Has.Some.Contains("Implementation guide 1.7 is ready"));
        }

        [Test]
        public void Check_json_output_deserializes_into_the_hook_check_result()
        {
            var layout = new RepositoryLayout(this.repositoryRoot);
            var store = new InstalledVersionsStore(layout.KnowledgeRoot);
            store.MarkFetched("1.2", setAsDefault: true);
            store.MarkGenerated("1.2", specGenerated: true);
            store.MarkGuideImported("1.7");
            store.MarkGuideGenerated("1.7", succeeded: true);
            store.MarkApiGenerated(new InstalledApi(BundledPackages.ReqIFSharpVersion, BundledPackages.ExtensionsVersion));

            var (exitCode, output) = ConsoleCapture.Run(CheckCommand.Build(), "--json", "--repository-root", this.repositoryRoot);
            var result = System.Text.Json.JsonSerializer.Deserialize(output, ReqIFSharp.Sage.Tools.Hook.HookJsonContext.Default.CheckResult)!;

            Assert.That(exitCode, Is.EqualTo(0));
            Assert.That(result.Default, Is.EqualTo("1.2"));
            Assert.That(result.HasAnyVersion, Is.True);
            Assert.That(result.Versions.Single().SpecGenerated, Is.True);
            Assert.That(result.Guides.Single().Generated, Is.True);
            Assert.That(result.Api!.ReqIFSharpVersion, Is.EqualTo(BundledPackages.ReqIFSharpVersion));
            Assert.That(result.ApiUpToDate, Is.True);
            Assert.That(result.BundledReqIFSharpVersion, Is.EqualTo(BundledPackages.ReqIFSharpVersion));
        }

        [Test]
        public void BuildReport_flags_an_api_reference_for_another_reqifsharp_version()
        {
            var layout = new RepositoryLayout(this.repositoryRoot);
            new InstalledVersionsStore(layout.KnowledgeRoot).MarkApiGenerated(new InstalledApi("1.0.0", "1.0.0"));

            var report = CheckCommand.BuildReport(layout);

            Assert.That(report.ApiUpToDate, Is.False);
            Assert.That(report.Summary, Has.Some.Contains("ReqIFSharp 1.0.0"));
        }
    }
}
