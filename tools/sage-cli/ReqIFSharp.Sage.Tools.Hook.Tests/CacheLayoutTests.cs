// -------------------------------------------------------------------------------------------------
// <copyright file="CacheLayoutTests.cs" company="Starion Group S.A.">
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
    [TestFixture]
    public class CacheLayoutTests
    {
        [Test]
        public void InstallKey_is_stable_for_the_same_path()
        {
            var first = CacheLayout.InstallKey(@"C:\repo\reqifsharp-sage");
            var second = CacheLayout.InstallKey(@"C:\repo\reqifsharp-sage");

            Assert.That(first, Is.EqualTo(second));
        }

        [Test]
        public void InstallKey_differs_for_different_paths()
        {
            var first = CacheLayout.InstallKey(@"C:\repo\checkout-one");
            var second = CacheLayout.InstallKey(@"C:\repo\checkout-two");

            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void CliDirectoryFor_includes_version_rid_and_install_key()
        {
            var directory = CacheLayout.CliDirectoryFor(@"C:\repo\reqifsharp-sage", "1.0.0", "win-x64");

            Assert.That(directory, Does.Contain("1.0.0"));
            Assert.That(directory, Does.Contain("win-x64"));
            Assert.That(directory, Does.Contain(CacheLayout.InstallKey(@"C:\repo\reqifsharp-sage")));
            Assert.That(directory, Does.StartWith(CacheLayout.CacheRoot));
        }
    }
}
