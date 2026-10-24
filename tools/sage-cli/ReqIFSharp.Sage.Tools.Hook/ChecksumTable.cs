// -------------------------------------------------------------------------------------------------
// <copyright file="ChecksumTable.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Hook
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Parses the per-asset SHA-256 checksum table the release workflow writes into each GitHub Release's
    /// body, one line per asset: <c>reqifsharp-sage-1.0.0-win-x64.zip sha256:&lt;64 hex chars&gt;</c>.
    /// </summary>
    public static partial class ChecksumTable
    {
        /// <summary>
        /// Parses <paramref name="releaseBody"/> into a map of asset file name to its expected SHA-256 hash.
        /// </summary>
        public static Dictionary<string, string> Parse(string releaseBody)
        {
            var result = new Dictionary<string, string>();

            foreach (Match match in ChecksumLine().Matches(releaseBody))
            {
                result[match.Groups["file"].Value] = match.Groups["sha256"].Value.ToLowerInvariant();
            }

            return result;
        }

        [GeneratedRegex(@"(?<file>[\w.\-]+\.zip)\s+sha256:(?<sha256>[0-9a-fA-F]{64})", RegexOptions.None)]
        private static partial Regex ChecksumLine();
    }
}
