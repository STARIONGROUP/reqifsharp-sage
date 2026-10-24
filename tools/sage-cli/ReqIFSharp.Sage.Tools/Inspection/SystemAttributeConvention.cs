// -------------------------------------------------------------------------------------------------
// <copyright file="SystemAttributeConvention.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Inspection
{
    /// <summary>
    /// One system attribute naming convention of the prostep ivip ReqIF Implementation Guide (§2.3): an attribute
    /// definition LONG-NAME, the kind of element whose SpecType defines it, and the recommended attribute definition kind.
    /// </summary>
    /// <param name="Name">The case-sensitive attribute definition LONG-NAME, e.g. "ReqIF.Text".</param>
    /// <param name="ElementKind">"SpecObject", "Specification" or "SpecRelation".</param>
    /// <param name="DefinitionKind">
    /// The recommended attribute definition kind, as the suffix of the XML element name: "STRING", "XHTML", "DATE",
    /// "BOOLEAN" or "ENUMERATION".
    /// </param>
    public sealed record SystemAttributeConvention(string Name, string ElementKind, string DefinitionKind);
}
