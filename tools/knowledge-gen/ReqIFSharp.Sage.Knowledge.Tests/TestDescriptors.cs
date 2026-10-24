// -------------------------------------------------------------------------------------------------
// <copyright file="TestDescriptors.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
