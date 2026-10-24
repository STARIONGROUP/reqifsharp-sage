// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionFixtures.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Tools.Tests.Inspection
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using ReqIFSharp.Sage.Tools.Inspection;

    /// <summary>
    /// Access to the toy fixtures under <c>Fixtures/Inspection</c> and helpers to derive variants of them in a temporary folder.
    /// </summary>
    internal static class InspectionFixtures
    {
        internal const string Clean = "clean.reqif";

        internal const string IdentifiersAndReferences = "identifiers-and-references.reqif";

        internal const string Values = "values.reqif";

        internal const string Xhtml = "xhtml.reqif";

        internal const string Guide = "guide.reqif";

        internal const string Malformed = "malformed.reqif";

        internal static readonly InspectionOptions WithGuide = new(IncludeGuideRules: true, GuideVersion: null);

        internal static readonly InspectionOptions WithoutGuide = new(IncludeGuideRules: false, GuideVersion: null);

        internal static string PathOf(string fixture) => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Inspection", fixture);

        internal static string Text(string fixture) => File.ReadAllText(PathOf(fixture));

        internal static InspectionReport Inspect(string fixture, InspectionOptions? options = null) => new ReqIFInspector().Inspect(PathOf(fixture), options ?? WithGuide);

        internal static string Describe(IEnumerable<InspectionFinding> findings) =>
            string.Join(Environment.NewLine, findings.Select(finding => $"{finding.Document} {finding.Line} {finding.Severity} {finding.RuleId} {finding.Identifier}: {finding.Message}"));

        /// <summary>
        /// The clean fixture with the text of an XHTML value replaced, e.g. to embed objects.
        /// </summary>
        internal static string CleanWithBrewText(string xhtml) => Text(Clean).Replace("The machine <xhtml:b>shall</xhtml:b> brew a cup of coffee.", xhtml, StringComparison.Ordinal);

        /// <summary>
        /// The clean fixture with a prostep ivip exchange conversation identifier in its tool extensions.
        /// </summary>
        internal static string WithConversation(string reqif, string conversationId) =>
            reqif.Replace(
                "</REQ-IF>",
                $"""
                  <TOOL-EXTENSIONS>
                    <REQ-IF-TOOL-EXTENSION>
                      <reqif-common:EXCHANGE-CONVERSATION xmlns:reqif-common="http://www.prostep.org/reqif">
                        <reqif-common:IDENTIFIER>{conversationId}</reqif-common:IDENTIFIER>
                      </reqif-common:EXCHANGE-CONVERSATION>
                    </REQ-IF-TOOL-EXTENSION>
                  </TOOL-EXTENSIONS>
                </REQ-IF>
                """,
                StringComparison.Ordinal);

        /// <summary>
        /// The clean fixture with its spec object req-brew renamed, while the relation still targets req-brew,
        /// so the relation target only resolves when another document defines req-brew.
        /// </summary>
        internal static string CleanWithForeignRelationTarget() =>
            Regex.Replace(Text(Clean).Replace("req-brew", "req-brew-2", StringComparison.Ordinal), @"<TARGET>\s*<SPEC-OBJECT-REF>req-brew-2</SPEC-OBJECT-REF>", "<TARGET><SPEC-OBJECT-REF>req-brew</SPEC-OBJECT-REF>");

        /// <summary>
        /// The clean fixture with an element reqifsharp does not know (FANCY-EXTRA) inside the spec object req-grind.
        /// </summary>
        internal static string CleanWithUnsupportedElement()
        {
            var text = Text(Clean);
            var index = text.IndexOf("<SPEC-OBJECT IDENTIFIER=\"req-grind\"", StringComparison.Ordinal);
            var insertAt = text.IndexOf('>', index) + 1;

            return text[..insertAt] + "<FANCY-EXTRA/>" + text[insertAt..];
        }

        internal static void WriteArchive(string path, params (string Name, byte[] Content)[] entries)
        {
            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);

            foreach (var (name, content) in entries)
            {
                using var stream = archive.CreateEntry(name).Open();
                stream.Write(content);
            }
        }

        internal static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
    }
}
