// -------------------------------------------------------------------------------------------------
// <copyright file="BackgroundProvisioningTests.cs" company="Starion Group S.A.">
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
    using System;
    using System.IO;
    using System.Text.Json;

    [TestFixture]
    public class BackgroundProvisioningTests
    {
        private string directory = null!;

        [SetUp]
        public void SetUp()
        {
            this.directory = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", Path.GetRandomFileName());
            Directory.CreateDirectory(this.directory);
        }

        [TearDown]
        public void TearDown()
        {
            Directory.Delete(this.directory, recursive: true);
        }

        [Test]
        public void IsRunning_is_false_without_a_lock_and_true_with_a_fresh_one()
        {
            var cliDirectory = Path.Combine(this.directory, "abc");

            Assert.That(BackgroundProvisioning.IsRunning(cliDirectory, DateTime.UtcNow), Is.False);

            File.WriteAllText(CacheLayout.ProvisioningLockPath(cliDirectory), "x");

            Assert.That(BackgroundProvisioning.IsRunning(cliDirectory, DateTime.UtcNow), Is.True);
        }

        [Test]
        public void IsRunning_ignores_a_lock_older_than_the_timeout()
        {
            var cliDirectory = Path.Combine(this.directory, "abc");
            var lockPath = CacheLayout.ProvisioningLockPath(cliDirectory);
            File.WriteAllText(lockPath, "x");

            Assert.That(BackgroundProvisioning.IsRunning(cliDirectory, File.GetLastWriteTimeUtc(lockPath) + BackgroundProvisioning.Timeout + TimeSpan.FromSeconds(1)), Is.False);
        }

        [Test]
        public void ReleaseLock_removes_the_lock_and_tolerates_a_missing_one()
        {
            var cliDirectory = Path.Combine(this.directory, "abc");
            File.WriteAllText(CacheLayout.ProvisioningLockPath(cliDirectory), "x");

            BackgroundProvisioning.ReleaseLock(cliDirectory);
            BackgroundProvisioning.ReleaseLock(cliDirectory);

            Assert.That(File.Exists(CacheLayout.ProvisioningLockPath(cliDirectory)), Is.False);
        }

        [Test]
        public void The_lock_file_sits_next_to_the_cli_directory_not_inside_it()
        {
            var cliDirectory = Path.Combine(this.directory, "abc") + Path.DirectorySeparatorChar;

            Assert.That(CacheLayout.ProvisioningLockPath(cliDirectory), Is.EqualTo(Path.Combine(this.directory, "abc.provisioning")));
        }

        [Test]
        public void TryGetCached_never_touches_the_network_and_is_null_for_an_unknown_version()
        {
            Assert.That(CliProvisioner.TryGetCached(this.directory, "0.0.0-not-provisioned"), Is.Null);
        }

        [Test]
        public void Provisioning_context_names_the_plugin_root_and_the_dotnet_run_fallback()
        {
            var message = CheckResultSummarizer.Provisioning("/plugins/reqifsharp-sage");

            Assert.That(message, Does.Contain("/plugins/reqifsharp-sage"));
            Assert.That(message, Does.Contain("downloaded in the background"));
            Assert.That(message, Does.Contain("dotnet run --project \"/plugins/reqifsharp-sage/tools/sage-cli/ReqIFSharp.Sage.Tools\""));
        }

        [Test]
        public void HookOutput_serializes_to_the_hookSpecificOutput_envelope_claude_code_reads()
        {
            var output = new HookOutput
            {
                HookSpecificOutput = new HookSpecificOutput { HookEventName = "SessionStart", AdditionalContext = "x" },
            };

            var json = JsonSerializer.Serialize(output, HookJsonContext.Default.HookOutput);

            Assert.That(json, Is.EqualTo("""{"hookSpecificOutput":{"hookEventName":"SessionStart","additionalContext":"x"}}"""));
        }
    }
}
