// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFInspector.cs" company="Starion Group S.A.">
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
    using System.IO.Compression;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    /// <summary>
    /// Inspects a user-supplied <c>.reqif</c> file or <c>.reqifz</c> archive for conformance to OMG ReqIF 1.2 and,
    /// optionally, to the prostep ivip ReqIF Implementation Guide. Never throws for a bad input file: every problem,
    /// including an unreadable file, comes back as a finding.
    /// </summary>
    /// <remarks>
    /// Each ReqIF document (the file, or each <c>.reqif</c> entry of an archive) goes through these passes:
    /// <list type="number">
    /// <item>XML parsing with line information; a document that is not well-formed gets one LOAD error and no further checks.</item>
    /// <item>XSD validation against the schema set embedded in reqifsharp (rule XSD, with line numbers).</item>
    /// <item>Deserialization with reqifsharp's <see cref="ReqIFDeserializer"/>; an exception is a LOAD error, a logged warning a READER finding.
    /// The semantic checks still run after a LOAD error caused by reqifsharp, since they usually explain it.</item>
    /// <item>The ReqIF 1.2 semantic rules (R-xxx) over the XML, see <see cref="ReqIFXmlDocument"/> for why the XML and not the object graph.</item>
    /// <item>When requested, the Implementation Guide rules (G-xxx).</item>
    /// </list>
    /// The rules, their severities and references are listed by <see cref="InspectionRuleCatalog"/>.
    /// </remarks>
    public sealed class ReqIFInspector
    {
        private readonly ILogger<ReqIFInspector> logger;

        private readonly SchemaValidator schemaValidator = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ReqIFInspector"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory for the inspector's own diagnostics; <c>null</c> for no logging.</param>
        public ReqIFInspector(ILoggerFactory? loggerFactory = null)
        {
            this.logger = loggerFactory?.CreateLogger<ReqIFInspector>() ?? NullLogger<ReqIFInspector>.Instance;
        }

        /// <summary>
        /// Inspects the <c>.reqif</c> file or <c>.reqifz</c> archive at <paramref name="path"/>. Whether the file is an
        /// archive is decided from its content, not its extension.
        /// </summary>
        /// <param name="path">The file to inspect.</param>
        /// <param name="options">The inspection options.</param>
        /// <returns>The report; never <c>null</c>.</returns>
        public InspectionReport Inspect(string path, InspectionOptions options)
        {
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(options);

            var context = new InspectionContext { IncludeGuideRules = options.IncludeGuideRules };
            var documents = new List<InspectedDocument>();

            try
            {
                this.InspectFile(path, options, context, documents);
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Inspection of {Path} failed unexpectedly", path);
                context.Document = null;
                context.Report(RuleIds.Inspect, $"The inspection stopped unexpectedly ({exception.GetType().Name}: {exception.Message}); the findings so far are reported.");
            }

            return new InspectionReport(path, documents, context.OrderedFindings());
        }

        private static bool IsZip(byte[] content) =>
            content.Length >= 4 && content[0] == 0x50 && content[1] == 0x4B && (content[2] == 0x03 || content[2] == 0x05) && (content[3] == 0x04 || content[3] == 0x06);

        private static string NormalizeEntryPath(string path)
        {
            var segments = new List<string>();

            foreach (var segment in path.Replace('\\', '/').Split('/'))
            {
                if (segment.Length == 0 || segment == ".")
                {
                    continue;
                }

                if (segment == "..")
                {
                    if (segments.Count > 0)
                    {
                        segments.RemoveAt(segments.Count - 1);
                    }

                    continue;
                }

                segments.Add(segment);
            }

            return string.Join('/', segments);
        }

        private static List<DocumentSource> ReadArchive(byte[] content, InspectionContext context)
        {
            using var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);

            var entryNames = archive.Entries
                .Where(entry => entry.FullName.Length > 0 && !entry.FullName.EndsWith('/'))
                .Select(entry => NormalizeEntryPath(entry.FullName))
                .ToHashSet(StringComparer.Ordinal);

            var sources = new List<DocumentSource>();

            foreach (var entry in archive.Entries.Where(entry => entry.Name.EndsWith(".reqif", StringComparison.OrdinalIgnoreCase)).OrderBy(entry => entry.FullName, StringComparer.Ordinal))
            {
                byte[] bytes;

                try
                {
                    using var entryStream = entry.Open();
                    using var buffer = new MemoryStream();
                    entryStream.CopyTo(buffer);
                    bytes = buffer.ToArray();
                }
                catch (Exception exception) when (exception is InvalidDataException or IOException or NotSupportedException)
                {
                    context.Document = entry.FullName;
                    context.Report(RuleIds.Load, $"The archive entry cannot be extracted ({exception.GetType().Name}: {exception.Message}); .reqifz archives must use standard ZIP (Deflate) compression.");
                    context.Document = null;
                    continue;
                }

                var directory = NormalizeEntryPath(entry.FullName);
                var slash = directory.LastIndexOf('/');
                directory = slash < 0 ? string.Empty : directory[..slash];

                sources.Add(new DocumentSource(
                    entry.FullName,
                    bytes,
                    relative => entryNames.Contains(NormalizeEntryPath(directory.Length == 0 ? relative : directory + "/" + relative)),
                    "the .reqifz archive"));
            }

            if (sources.Count == 0 && !context.OrderedFindings().Any(finding => finding.RuleId == RuleIds.Load))
            {
                context.Report(RuleIds.Load, "The archive contains no .reqif entry, so there is no ReqIF document to inspect.");
            }

            return sources;
        }

        private static XDocument? Parse(DocumentSource source, InspectionContext context)
        {
            if (source.Content.Length == 0)
            {
                context.Report(RuleIds.Load, "The document is empty.");
                return null;
            }

            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };

            try
            {
                using var stream = new MemoryStream(source.Content, writable: false);
                using var reader = XmlReader.Create(stream, settings);
                var xml = XDocument.Load(reader, LoadOptions.SetLineInfo);

                if (xml.Root is null || xml.Root.Name.LocalName != "REQ-IF")
                {
                    context.Report(RuleIds.Load, $"The root element is <{xml.Root?.Name.LocalName}>, not <REQ-IF>; this is not a ReqIF document.", line: xml.Root is null ? null : ReqIFXml.Line(xml.Root));
                    return null;
                }

                if (!ReqIFXml.IsReqIF(xml.Root))
                {
                    context.Report(RuleIds.Load, $"The REQ-IF root element is in the namespace '{xml.Root.Name.NamespaceName}' instead of {ReqIFXml.ReqIFNamespace}; reqifsharp skips it, so nothing is read.", line: ReqIFXml.Line(xml.Root));
                    return null;
                }

                if (xml.Root.Name.NamespaceName.Length == 0)
                {
                    context.Report(RuleIds.Load, $"The REQ-IF root element has no namespace; it must be in {ReqIFXml.ReqIFNamespace}. reqifsharp tolerates this, but other tools do not, and XML schema validation was skipped.", line: ReqIFXml.Line(xml.Root), severity: InspectionSeverity.Warning);
                }

                return xml;
            }
            catch (XmlException exception)
            {
                context.Report(RuleIds.Load, $"The document is not well-formed XML: {exception.Message}", line: exception.LineNumber > 0 ? exception.LineNumber : null);
                return null;
            }
        }

        private void InspectFile(string path, InspectionOptions options, InspectionContext context, List<InspectedDocument> documents)
        {
            if (options.IncludeGuideRules && options.GuideVersion is { } requested && requested != InspectionRuleCatalog.GuideVersion)
            {
                context.Report(RuleIds.GuideVersion, $"Implementation Guide version {requested} is not implemented; the rules of version {InspectionRuleCatalog.GuideVersion} were applied.");
            }

            if (Directory.Exists(path))
            {
                context.Report(RuleIds.Load, $"'{path}' is a directory, not a .reqif or .reqifz file.");
                return;
            }

            if (!File.Exists(path))
            {
                context.Report(RuleIds.Load, $"The file '{path}' does not exist.");
                return;
            }

            byte[] content;

            try
            {
                content = File.ReadAllBytes(path);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                context.Report(RuleIds.Load, $"The file cannot be read ({exception.GetType().Name}: {exception.Message}).");
                return;
            }

            var isArchive = IsZip(content);

            GuideChecker.CheckFileExtension(path, isArchive, context);

            List<DocumentSource> sources;

            if (isArchive)
            {
                try
                {
                    sources = ReadArchive(content, context);
                }
                catch (InvalidDataException exception)
                {
                    context.Report(RuleIds.Load, $"The file looks like a ZIP archive but cannot be opened as one ({exception.Message}).");
                    return;
                }
            }
            else
            {
                var directory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? string.Empty;

                sources =
                [
                    new DocumentSource(
                        Path.GetFileName(path),
                        content,
                        relative => File.Exists(Path.Combine(directory, relative.Replace('/', Path.DirectorySeparatorChar))),
                        "the folder of the .reqif file"),
                ];
            }

            var parsed = new List<ReqIFXmlDocument>();

            foreach (var source in sources)
            {
                context.Document = source.Name;
                this.logger.LogDebug("Inspecting {Document}", source.Name);

                GuideChecker.CheckEncoding(source, context);

                var xml = Parse(source, context);

                if (xml is null)
                {
                    documents.Add(new InspectedDocument(null, null, null, null, null, 0, 0, 0, 0, 0, source.Name));
                    continue;
                }

                if (xml.Root!.Name.NamespaceName == ReqIFXml.ReqIFNamespace)
                {
                    this.Guarded(context, "XML schema validation", () => this.schemaValidator.Validate(source, context));
                }

                this.Guarded(context, "reqifsharp deserialization", () => ReqIFSharpLoadCheck.Run(source, context));

                var document = new ReqIFXmlDocument(source, xml);
                parsed.Add(document);
                documents.Add(document.ToInspectedDocument());
            }

            var archiveIdentifiers = parsed.SelectMany(document => document.Identifiers).ToHashSet(StringComparer.Ordinal);

            foreach (var document in parsed)
            {
                context.Document = document.Name;

                this.Guarded(context, "the ReqIF 1.2 rules", () => new SemanticChecker(document, context, archiveIdentifiers, isArchive).Run());
                this.Guarded(context, "the XHTML rules", () => new XhtmlChecker(document, context).Run());

                if (options.IncludeGuideRules)
                {
                    this.Guarded(context, "the Implementation Guide rules", () => GuideChecker.Run(document, context));
                }
            }

            context.Document = null;

            if (options.IncludeGuideRules && parsed.Count > 1)
            {
                GuideChecker.CheckConversationIdentifiers(parsed, context);
            }
        }

        private void Guarded(InspectionContext context, string what, Action check)
        {
            try
            {
                check();
            }
            catch (Exception exception)
            {
                this.logger.LogError(exception, "Running {What} on {Document} failed", what, context.Document);
                context.Report(RuleIds.Inspect, $"Running {what} failed ({exception.GetType().Name}: {exception.Message}); its findings may be incomplete.");
            }
        }
    }
}
