// -------------------------------------------------------------------------------------------------
// <copyright file="RegistryTests.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Knowledge.Tests
{
    using System.IO;
    using System.Linq;
    using System.Text.Json;

    [TestFixture]
    public class RegistryTests
    {
        [Test]
        public void The_committed_registry_json_mirrors_the_known_versions_in_code()
        {
            var path = FindRegistry();
            using var document = JsonDocument.Parse(File.ReadAllText(path));

            var reqif = document.RootElement.GetProperty("reqifVersions").EnumerateArray().Select(v => v.GetProperty("version").GetString()).ToList();
            var guides = document.RootElement.GetProperty("guideVersions").EnumerateArray().Select(v => v.GetProperty("version").GetString()).ToList();

            Assert.That(reqif, Is.EqualTo(KnownReqIFVersions.All.Select(v => v.Version)));
            Assert.That(guides, Is.EqualTo(KnownGuideVersions.All.Select(v => v.Version)));
        }

        [Test]
        public void Every_known_reqif_version_has_one_file_per_role_with_a_sha256()
        {
            foreach (var descriptor in KnownReqIFVersions.All)
            {
                Assert.That(descriptor.Files.Select(f => f.Role).Distinct().Count(), Is.EqualTo(descriptor.Files.Count), descriptor.Version);
                Assert.That(descriptor.Files.All(f => f.Sha256.Length == 64), Is.True, descriptor.Version);
            }

            Assert.That(KnownReqIFVersions.Current.Version, Is.EqualTo("1.2"));
        }

        private static string FindRegistry()
        {
            for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory is not null; directory = directory.Parent)
            {
                var candidate = Path.Combine(directory.FullName, "knowledge", "registry.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            Assert.Fail("knowledge/registry.json was not found above the test directory.");
            return string.Empty;
        }
    }
}
