// -------------------------------------------------------------------------------------------------
// <copyright file="SchemaValidator.cs" company="Starion Group S.A.">
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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Resources;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Validates a document against the ReqIF XML schema set embedded in the reqifsharp assembly - the same
    /// <c>reqif.xsd</c> and XHTML driver, resolved through <see cref="ReqIfSchemaResolver"/>, that
    /// <see cref="ReqIFDeserializer"/> uses when asked to validate.
    /// </summary>
    /// <remarks>
    /// The validation runs as a separate pass rather than through <c>ReqIFDeserializer.Deserialize(..., validate: true, ...)</c>:
    /// that call reports through the handler without aborting, but cannot tell which archive entry an event belongs to,
    /// and it also processes xsi:schemaLocation hints. Here no schema location is followed and no network access is made.
    /// </remarks>
    internal sealed class SchemaValidator
    {
        private const string SchemaResourceName = "ReqIFSharp.Resources.reqif.xsd";

        private readonly Lazy<XmlSchemaSet> schemaSet = new(CreateSchemaSet);

        /// <summary>
        /// Validates <paramref name="source"/>, reporting every schema error and warning (rule XSD) with its line.
        /// </summary>
        /// <param name="source">The document.</param>
        /// <param name="context">The context to report to.</param>
        internal void Validate(DocumentSource source, InspectionContext context)
        {
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
            };

            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.Schemas.Add(this.schemaSet.Value);

            // Each open element with the IDENTIFIER of its nearest identifiable ancestor-or-self.
            var open = new Stack<(string LocalName, string? Identifier)>();
            XmlReader? reader = null;

            settings.ValidationEventHandler += (_, args) =>
            {
                if (args.Severity == XmlSeverityType.Warning && open.Any(element => element.LocalName == "TOOL-EXTENSIONS"))
                {
                    // Tool extension content is processed laxly by design; "no schema information" warnings there are noise.
                    return;
                }

                string? identifier = null;

                if (reader is { NodeType: XmlNodeType.Element })
                {
                    identifier = reader.LocalName == "ALTERNATIVE-ID" ? null : reader.GetAttribute("IDENTIFIER");
                }

                identifier ??= open.Count > 0 ? open.Peek().Identifier : null;

                int? line = args.Exception?.LineNumber > 0 ? args.Exception.LineNumber : null;

                context.Report(
                    RuleIds.Xsd,
                    $"XML schema {(args.Severity == XmlSeverityType.Error ? "error" : "warning")}: {args.Message}",
                    identifier: identifier,
                    line: line,
                    severity: args.Severity == XmlSeverityType.Error ? InspectionSeverity.Error : InspectionSeverity.Warning);
            };

            using var stream = new MemoryStream(source.Content, writable: false);
            reader = XmlReader.Create(stream, settings);

            using (reader)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && !reader.IsEmptyElement)
                    {
                        var own = reader.LocalName == "ALTERNATIVE-ID" ? null : reader.GetAttribute("IDENTIFIER");
                        open.Push((reader.LocalName, own ?? (open.Count > 0 ? open.Peek().Identifier : null)));
                    }
                    else if (reader.NodeType == XmlNodeType.EndElement && open.Count > 0)
                    {
                        open.Pop();
                    }
                }
            }
        }

        private static XmlSchemaSet CreateSchemaSet()
        {
            var assembly = typeof(ReqIFDeserializer).Assembly;

            using var stream = assembly.GetManifestResourceStream(SchemaResourceName)
                               ?? throw new MissingManifestResourceException($"The {SchemaResourceName} resource could not be found in {assembly.FullName}.");

            var schemaSet = new XmlSchemaSet { XmlResolver = new ReqIfSchemaResolver() };
            schemaSet.Add(XmlSchema.Read(stream, null)!);
            schemaSet.Compile();

            return schemaSet;
        }
    }
}
