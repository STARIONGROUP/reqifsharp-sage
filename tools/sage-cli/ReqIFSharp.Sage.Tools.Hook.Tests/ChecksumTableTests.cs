// -------------------------------------------------------------------------------------------------
// <copyright file="ChecksumTableTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Hook.Tests
{
    [TestFixture]
    public class ChecksumTableTests
    {
        [Test]
        public void Parse_extracts_one_entry_per_asset_line()
        {
            var body = """
                ## reqifsharp-sage tools v1.0.0

                Checksums:

                reqifsharp-sage-1.0.0-win-x64.zip sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
                reqifsharp-sage-1.0.0-linux-x64.zip sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
                """;

            var checksums = ChecksumTable.Parse(body);

            Assert.That(checksums, Has.Count.EqualTo(2));
            Assert.That(checksums["reqifsharp-sage-1.0.0-win-x64.zip"], Is.EqualTo(new string('a', 64)));
            Assert.That(checksums["reqifsharp-sage-1.0.0-linux-x64.zip"], Is.EqualTo(new string('b', 64)));
        }

        [Test]
        public void Parse_lowercases_the_hash()
        {
            var body = $"reqifsharp-sage-1.0.0-osx-arm64.zip sha256:{new string('A', 64)}";

            var checksums = ChecksumTable.Parse(body);

            Assert.That(checksums["reqifsharp-sage-1.0.0-osx-arm64.zip"], Is.EqualTo(new string('a', 64)));
        }

        [Test]
        public void Parse_returns_empty_for_a_body_with_no_checksum_lines()
        {
            Assert.That(ChecksumTable.Parse("Just some release notes, no checksums here."), Is.Empty);
        }
    }
}
