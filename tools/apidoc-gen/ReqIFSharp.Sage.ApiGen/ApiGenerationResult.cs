// -------------------------------------------------------------------------------------------------
// <copyright file="ApiGenerationResult.cs" company="Starion Group S.A.">
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
