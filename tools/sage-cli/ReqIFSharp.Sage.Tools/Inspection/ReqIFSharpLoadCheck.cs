// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFSharpLoadCheck.cs" company="Starion Group S.A.">
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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Deserializes a document with reqifsharp's own <see cref="ReqIFDeserializer"/> (without validation) to find out
    /// whether applications built on reqifsharp can read it, and reports what reqifsharp logs while doing so.
    /// </summary>
    internal static partial class ReqIFSharpLoadCheck
    {
        /// <summary>
        /// Every element name the ReqIF schema declares. reqifsharp 10.1.2 logs "The X element ... is not supported"
        /// for the end tag of most elements it reads through a sub-tree reader; such warnings about elements the
        /// schema knows are noise and are dropped, while warnings about unknown elements are kept.
        /// </summary>
        private static readonly Lazy<HashSet<string>> SchemaElementNames = new(LoadSchemaElementNames);

        /// <summary>
        /// Deserializes <paramref name="source"/>: an exception becomes a LOAD error, a logged warning/error a READER finding.
        /// </summary>
        /// <param name="source">The document.</param>
        /// <param name="context">The context to report to.</param>
        /// <returns><c>true</c> when reqifsharp read exactly one ReqIF document.</returns>
        internal static bool Run(DocumentSource source, InspectionContext context)
        {
            var provider = new CapturingLoggerProvider();
            var loaded = false;

            using (var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(provider).SetMinimumLevel(LogLevel.Warning)))
            {
                try
                {
                    using var stream = new MemoryStream(source.Content, writable: false);
                    var reqifs = new ReqIFDeserializer(loggerFactory).Deserialize(stream, SupportedFileExtensionKind.Reqif).ToList();

                    if (reqifs.Count == 0)
                    {
                        context.Report(RuleIds.Load, "reqifsharp found no REQ-IF root element in the ReqIF namespace, so it reads nothing from this document.");
                    }
                    else
                    {
                        loaded = true;
                    }
                }
                catch (Exception exception)
                {
                    var message = exception.Message;

                    if (exception.InnerException is not null)
                    {
                        message += $" ({exception.InnerException.GetType().Name}: {exception.InnerException.Message})";
                    }

                    context.Report(
                        RuleIds.Load,
                        $"reqifsharp cannot deserialize this document ({exception.GetType().Name}: {message}). Applications built on reqifsharp will fail to open it; the other findings point to the likely cause.",
                        line: LineOf(message));
                }
            }

            foreach (var (level, message) in provider.Entries.Distinct())
            {
                var unsupported = UnsupportedPattern().Match(message);

                if (unsupported.Success && SchemaElementNames.Value.Contains(unsupported.Groups[1].Value))
                {
                    continue;
                }

                context.Report(
                    RuleIds.Reader,
                    $"reqifsharp logged: {message}",
                    line: LineOf(message),
                    severity: level >= LogLevel.Error ? InspectionSeverity.Error : InspectionSeverity.Warning);
            }

            return loaded;
        }

        private static int? LineOf(string message)
        {
            var match = LinePattern().Match(message);

            return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var line) && line > 0 ? line : null;
        }

        private static HashSet<string> LoadSchemaElementNames()
        {
            using var stream = typeof(ReqIFDeserializer).Assembly.GetManifestResourceStream("ReqIFSharp.Resources.reqif.xsd");

            if (stream is null)
            {
                return [];
            }

            XNamespace xsd = "http://www.w3.org/2001/XMLSchema";

            return XDocument.Load(stream).Descendants(xsd + "element")
                .Select(element => element.Attribute("name")?.Value)
                .OfType<string>()
                .ToHashSet(StringComparer.Ordinal);
        }

        [GeneratedRegex(@"line:position (\d+):\d+")]
        private static partial Regex LinePattern();

        [GeneratedRegex(@"^The (\S+) element at line:position \S* is not supported$")]
        private static partial Regex UnsupportedPattern();
    }
}
