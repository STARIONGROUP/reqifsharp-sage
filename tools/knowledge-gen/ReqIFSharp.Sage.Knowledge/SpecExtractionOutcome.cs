// -------------------------------------------------------------------------------------------------
// <copyright file="SpecExtractionOutcome.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    /// <summary>
    /// The outcome of the <c>spec</c> generation step, which never throws - it either succeeds or is
    /// skipped with a human-readable reason (PDFs not fetched, Python unavailable, extraction failed).
    /// </summary>
    public sealed record SpecExtractionOutcome(bool Succeeded, string? SkippedReason)
    {
        /// <summary>
        /// Creates a successful outcome.
        /// </summary>
        public static SpecExtractionOutcome Ok() => new(true, null);

        /// <summary>
        /// Creates a skipped outcome with the given human-readable reason.
        /// </summary>
        public static SpecExtractionOutcome Skipped(string reason) => new(false, reason);
    }
}
