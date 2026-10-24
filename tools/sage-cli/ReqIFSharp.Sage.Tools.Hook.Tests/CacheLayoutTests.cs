// -------------------------------------------------------------------------------------------------
// <copyright file="CacheLayoutTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
