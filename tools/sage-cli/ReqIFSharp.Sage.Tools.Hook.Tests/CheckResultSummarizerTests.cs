// -------------------------------------------------------------------------------------------------
// <copyright file="CheckResultSummarizerTests.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Hook.Tests
{
    using System.IO;
    using System.Text.Json;

    [TestFixture]
    public class CheckResultSummarizerTests
    {
        private const string Cli = "/cache/reqifsharp-sage";

        private const string Root = "/plugins/reqifsharp-sage";

        [Test]
        public void Summarize_without_a_cli_is_null()
        {
            Assert.That(CheckResultSummarizer.Summarize(new CheckResult(), null, Root), Is.Null);
        }

        [Test]
        public void Summarize_always_states_how_to_run_the_cli()
        {
            var message = CheckResultSummarizer.Summarize(Ready(), Cli, Root);

            Assert.That(message, Does.Contain(Cli));
            Assert.That(message, Does.Contain($"--repository-root \"{Root}\""));
        }

        [Test]
        public void Summarize_when_the_check_failed_still_gives_the_cli_path()
        {
            var message = CheckResultSummarizer.Summarize(null, Cli, Root);

            Assert.That(message, Does.Contain(Cli));
            Assert.That(message, Does.Contain("check"));
        }

        [Test]
        public void Summarize_offers_setup_when_nothing_is_installed()
        {
            Assert.That(CheckResultSummarizer.Summarize(new CheckResult(), Cli, Root), Does.Contain("knowledge-setup"));
        }

        [Test]
        public void Summarize_offers_generate_when_only_fetched()
        {
            var result = new CheckResult { Default = "1.2", HasAnyVersion = true, Versions = [new CheckResultVersion { Version = "1.2", Fetched = true }] };

            Assert.That(CheckResultSummarizer.Summarize(result, Cli, Root), Does.Contain("fetched but not generated"));
        }

        [Test]
        public void Summarize_mentions_clause_numbers_only_when_spec_text_is_missing()
        {
            var result = Ready();
            result.Versions[0].SpecGenerated = false;

            Assert.That(CheckResultSummarizer.Summarize(result, Cli, Root), Does.Contain("clause numbers only"));
        }

        [Test]
        public void Summarize_mentions_the_guide_is_optional_and_user_supplied_when_not_imported()
        {
            var result = Ready();
            result.Guides.Clear();

            Assert.That(CheckResultSummarizer.Summarize(result, Cli, Root), Does.Contain("import-guide"));
        }

        [Test]
        public void Summarize_flags_a_stale_api_reference()
        {
            var result = Ready();
            result.ApiUpToDate = false;
            result.BundledReqIFSharpVersion = "11.0.0";

            Assert.That(CheckResultSummarizer.Summarize(result, Cli, Root), Does.Contain("11.0.0"));
        }

        [Test]
        public void Summarize_of_a_fully_ready_install_only_reports_ready()
        {
            var message = CheckResultSummarizer.Summarize(Ready(), Cli, Root);

            Assert.That(message, Does.Contain("knowledge is ready"));
            Assert.That(message, Does.Not.Contain("offer"));
        }

        [Test]
        public void The_check_json_written_by_the_cli_in_camel_case_deserializes()
        {
            const string json = """{"default":"1.2","hasAnyVersion":true,"versions":[{"version":"1.2","fetched":true,"generated":true,"specGenerated":true}],"guides":[{"version":"1.7","imported":true,"generated":false}],"api":{"reqIFSharpVersion":"10.1.2","extensionsVersion":"5.2.2"},"apiUpToDate":true,"bundledReqIFSharpVersion":"10.1.2"}""";

            var result = JsonSerializer.Deserialize(json, HookJsonContext.Default.CheckResult)!;

            Assert.That(result.Versions[0].SpecGenerated, Is.True);
            Assert.That(result.Guides[0].Imported, Is.True);
            Assert.That(result.Api!.ReqIFSharpVersion, Is.EqualTo("10.1.2"));
        }

        [Test]
        public void LocalCliLocator_finds_a_build_in_the_checkout()
        {
            var root = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
            var bin = Path.Combine(root, "tools", "sage-cli", "ReqIFSharp.Sage.Tools", "bin", "Debug", "net10.0");
            Directory.CreateDirectory(bin);
            File.WriteAllText(Path.Combine(bin, "reqifsharp-sage.exe"), string.Empty);

            try
            {
                Assert.That(LocalCliLocator.Find(root, "reqifsharp-sage.exe"), Is.EqualTo(Path.Combine(bin, "reqifsharp-sage.exe")));
                Assert.That(LocalCliLocator.Find(root, "other"), Is.Null);
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        private static CheckResult Ready()
        {
            return new CheckResult
            {
                Default = "1.2",
                HasAnyVersion = true,
                Versions = [new CheckResultVersion { Version = "1.2", Fetched = true, Generated = true, SpecGenerated = true }],
                Guides = [new CheckResultGuide { Version = "1.7", Imported = true, Generated = true }],
                Api = new CheckResultApi { ReqIFSharpVersion = "10.1.2" },
                ApiUpToDate = true,
                BundledReqIFSharpVersion = "10.1.2",
            };
        }
    }
}
