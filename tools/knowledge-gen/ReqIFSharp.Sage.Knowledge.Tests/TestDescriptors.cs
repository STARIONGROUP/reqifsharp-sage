// -------------------------------------------------------------------------------------------------
// <copyright file="TestDescriptors.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Knowledge.Tests
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// A fictional ReqIF version descriptor pointing at example.com, and the toy fixture files.
    /// </summary>
    internal static class TestDescriptors
    {
        public static readonly byte[] PdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4 fake");

        public static readonly byte[] CmofBytes = Encoding.UTF8.GetBytes("<xmi:XMI xmlns:xmi=\"http://schema.omg.org/spec/XMI/2.1\"/>");

        public static readonly byte[] XsdBytes = Encoding.UTF8.GetBytes("<xsd:schema xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"/>");

        public static readonly byte[] DriverBytes = Encoding.UTF8.GetBytes("<xsd:schema xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><!-- driver --></xsd:schema>");

        public static ReqIFVersionDescriptor Fake => new(
            "1.2",
            true,
            "formal/16-07-01",
            [
                new SourceFile(SourceFileRole.SpecificationPdf, "https://example.com/PDF", "specs/ReqIF-1.2.pdf", Sha(PdfBytes)),
                new SourceFile(SourceFileRole.Metamodel, "https://example.com/reqif.cmof", "cmof/reqif.cmof", Sha(CmofBytes)),
                new SourceFile(SourceFileRole.Schema, "https://example.com/reqif.xsd", "schema/reqif.xsd", Sha(XsdBytes)),
                new SourceFile(SourceFileRole.XhtmlDriverSchema, "https://example.com/driver.xsd", "schema/driver.xsd", Sha(DriverBytes)),
            ]);

        public static string ToyFixture(string fileName) => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Toy", fileName);

        public static string Sha(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
    }
}
