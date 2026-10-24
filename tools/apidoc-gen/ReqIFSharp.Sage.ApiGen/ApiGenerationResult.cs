// -------------------------------------------------------------------------------------------------
// <copyright file="ApiGenerationResult.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen
{
    using System.Collections.Generic;

    /// <summary>
    /// The outcome of <see cref="ApiGenerator.Generate(string)"/>.
    /// </summary>
    /// <param name="TypeCount">The number of public types documented (one <c>types/*.md</c> page each).</param>
    /// <param name="ReqIFSharpVersion">The documented ReqIFSharp package version, e.g. <c>10.1.2</c>.</param>
    /// <param name="ExtensionsVersion">The documented ReqIFSharp.Extensions package version, e.g. <c>5.2.2</c>,
    /// or <see langword="null"/> when that package was not documented.</param>
    /// <param name="OutputDirectory">The directory the knowledge was written to.</param>
    /// <param name="FileCount">The number of files written.</param>
    /// <param name="Warnings">Non-fatal problems, e.g. a missing XML documentation file.</param>
    public sealed record ApiGenerationResult(
        int TypeCount,
        string ReqIFSharpVersion,
        string? ExtensionsVersion,
        string OutputDirectory,
        int FileCount,
        IReadOnlyList<string> Warnings);
}
