// -------------------------------------------------------------------------------------------------
// <copyright file="KnownReqIFVersions.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Knowledge
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The hand-maintained registry of OMG ReqIF versions this tool can fetch.
    /// </summary>
    /// <remarks>
    /// ReqIF 1.0.1, 1.1 and 1.2 all share the XML namespace <c>http://www.omg.org/spec/ReqIF/20110401/reqif.xsd</c>;
    /// only 1.2 (the version reqifsharp implements) is registered. The URLs are the machine-consumable
    /// files listed on the specification's cover page. Adding a version is a one-entry change here.
    /// </remarks>
    public static class KnownReqIFVersions
    {
        /// <summary>
        /// Every ReqIF version this tool can fetch and generate a knowledge base for.
        /// </summary>
        public static readonly IReadOnlyList<ReqIFVersionDescriptor> All =
        [
            new ReqIFVersionDescriptor(
                Version: "1.2",
                IsCurrent: true,
                OmgDocumentId: "formal/16-07-01",
                Files:
                [
                    new SourceFile(SourceFileRole.SpecificationPdf, "https://www.omg.org/spec/ReqIF/1.2/PDF", "specs/ReqIF-1.2.pdf", "c9d4fc54a13c5bd931c1a8eb039c77494d441e98a2be889ff3e9e96c3d299a2f"),
                    new SourceFile(SourceFileRole.Metamodel, "https://www.omg.org/spec/ReqIF/20101201/reqif.cmof", "cmof/reqif.cmof", "d0b998a59b65222d41add1e9bba420780a4fa7f9c9bf2c984dd9f5be8c2338e6"),
                    new SourceFile(SourceFileRole.Schema, "https://www.omg.org/spec/ReqIF/20110401/reqif.xsd", "schema/reqif.xsd", "9243f345540f25db3b53403da9ad9cd4744277ef01492ac3589937f533ba94c0"),
                    new SourceFile(SourceFileRole.XhtmlDriverSchema, "https://www.omg.org/spec/ReqIF/20110402/driver.xsd", "schema/driver.xsd", "4995bc97cf0a9b8462ca295006dd54d9a85fb820cf9fd6e134a51743fc44effd"),
                ]),
        ];

        /// <summary>
        /// Gets the version marked <see cref="ReqIFVersionDescriptor.IsCurrent"/>.
        /// </summary>
        public static ReqIFVersionDescriptor Current => All.First(v => v.IsCurrent);

        /// <summary>
        /// Finds a known version by its version string.
        /// </summary>
        /// <param name="version">The version, e.g. <c>1.2</c>.</param>
        /// <param name="descriptor">The descriptor, when found.</param>
        /// <returns>Whether the version is known.</returns>
        public static bool TryFind(string version, out ReqIFVersionDescriptor? descriptor)
        {
            descriptor = All.FirstOrDefault(v => v.Version == version);
            return descriptor is not null;
        }
    }
}
