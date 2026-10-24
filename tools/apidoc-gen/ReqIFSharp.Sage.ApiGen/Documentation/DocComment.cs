// -------------------------------------------------------------------------------------------------
// <copyright file="DocComment.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.ApiGen.Documentation
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    /// <summary>
    /// The resolved (post-<c>inheritdoc</c>) XML documentation of one type or member, still as XML so it can be
    /// rendered either with links (markdown pages) or without (JSON). Every part is optional.
    /// </summary>
    /// <param name="Summary">The <c>&lt;summary&gt;</c> element.</param>
    /// <param name="Remarks">The <c>&lt;remarks&gt;</c> element.</param>
    /// <param name="Returns">The <c>&lt;returns&gt;</c> element.</param>
    /// <param name="Value">The <c>&lt;value&gt;</c> element (properties).</param>
    /// <param name="Example">The <c>&lt;example&gt;</c> element.</param>
    /// <param name="Parameters">The <c>&lt;param&gt;</c> elements by parameter name.</param>
    /// <param name="TypeParameters">The <c>&lt;typeparam&gt;</c> elements by type parameter name.</param>
    /// <param name="Exceptions">The <c>&lt;exception&gt;</c> elements, in document order.</param>
    /// <param name="InheritedFrom">The documentation id the comment was (partly) inherited from, if any.</param>
    public sealed record DocComment(
        XElement? Summary,
        XElement? Remarks,
        XElement? Returns,
        XElement? Value,
        XElement? Example,
        IReadOnlyDictionary<string, XElement> Parameters,
        IReadOnlyDictionary<string, XElement> TypeParameters,
        IReadOnlyList<XElement> Exceptions,
        string? InheritedFrom)
    {
        /// <summary>
        /// Gets the empty comment of an undocumented member.
        /// </summary>
        public static DocComment Empty { get; } = new(
            null,
            null,
            null,
            null,
            null,
            new Dictionary<string, XElement>(StringComparer.Ordinal),
            new Dictionary<string, XElement>(StringComparer.Ordinal),
            Array.Empty<XElement>(),
            null);

        /// <summary>
        /// Gets a value indicating whether the comment carries no documentation at all.
        /// </summary>
        public bool IsEmpty => this.Summary is null && this.Remarks is null && this.Returns is null && this.Value is null
            && this.Example is null && this.Parameters.Count == 0 && this.TypeParameters.Count == 0 && this.Exceptions.Count == 0;
    }
}
