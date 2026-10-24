// -------------------------------------------------------------------------------------------------
// <copyright file="KnowledgeGenerationService.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using ReqIFSharp.Sage.ApiGen;
    using ReqIFSharp.Sage.MetamodelGen;

    /// <summary>
    /// Orchestrates the knowledge-base steps behind the CLI's <c>fetch</c> and <c>generate</c> verbs.
    /// </summary>
    public sealed class KnowledgeGenerationService
    {
        /// <summary>
        /// The <c>--document</c> name the ReqIF specification is extracted with.
        /// </summary>
        public const string SpecificationDocument = "ReqIF";

        /// <summary>
        /// The <c>--document</c> name the prostep implementation guide is extracted with.
        /// </summary>
        public const string GuideDocument = "PSI-Guide";

        private readonly SourceFetcher sourceFetcher;

        private readonly PythonSpecExtractRunner specExtractRunner;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeGenerationService"/> class.
        /// </summary>
        /// <param name="sourceFetcher">Stores the OMG source files.</param>
        /// <param name="specExtractRunner">Runs the Python PDF extraction.</param>
        public KnowledgeGenerationService(SourceFetcher sourceFetcher, PythonSpecExtractRunner specExtractRunner)
        {
            this.sourceFetcher = sourceFetcher;
            this.specExtractRunner = specExtractRunner;
        }

        /// <summary>
        /// Stores the source files of <paramref name="descriptor"/> and records the fetch.
        /// </summary>
        /// <param name="layout">The repository layout.</param>
        /// <param name="descriptor">The ReqIF version.</param>
        /// <param name="includeSpecs">Whether to include the specification PDF.</param>
        /// <param name="fromDirectory">A local directory to take the files from, or null to download.</param>
        /// <param name="setAsDefault">Whether to make the version the default.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The manifest entries.</returns>
        public async Task<IReadOnlyList<FetchManifestEntry>> FetchAsync(RepositoryLayout layout, ReqIFVersionDescriptor descriptor, bool includeSpecs, string? fromDirectory, bool setAsDefault, CancellationToken cancellationToken = default)
        {
            var entries = await this.sourceFetcher.FetchAsync(descriptor, layout.SourcesDirectoryFor(descriptor.Version), includeSpecs, fromDirectory, cancellationToken);
            new InstalledVersionsStore(layout.KnowledgeRoot).MarkFetched(descriptor.Version, setAsDefault);
            return entries;
        }

        /// <summary>
        /// Generates the knowledge of <paramref name="descriptor"/> from its stored sources: metamodel and
        /// schema pages, the specification text (when the PDF and Python are available), the text of every
        /// imported implementation guide, the reqifsharp API reference, and <c>datapackage.json</c>.
        /// </summary>
        /// <param name="layout">The repository layout.</param>
        /// <param name="descriptor">The ReqIF version.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>What was generated or skipped.</returns>
        /// <exception cref="FileNotFoundException">The metamodel has not been fetched.</exception>
        public async Task<GenerateOutcome> GenerateAsync(RepositoryLayout layout, ReqIFVersionDescriptor descriptor, CancellationToken cancellationToken = default)
        {
            var cmofPath = layout.SourcePathFor(descriptor.Version, descriptor.FileFor(SourceFileRole.Metamodel));
            if (!File.Exists(cmofPath))
            {
                throw new FileNotFoundException($"The ReqIF {descriptor.Version} metamodel was not found at {cmofPath} - run 'fetch --version {descriptor.Version}' first.", cmofPath);
            }

            var knowledgeDirectory = layout.KnowledgeDirectoryFor(descriptor.Version);
            var metamodel = MetamodelGenerator.Generate(
                cmofPath,
                layout.SourcePathFor(descriptor.Version, descriptor.FileFor(SourceFileRole.Schema)),
                layout.SourcePathFor(descriptor.Version, descriptor.FileFor(SourceFileRole.XhtmlDriverSchema)),
                knowledgeDirectory);

            var spec = await this.specExtractRunner.ExtractAsync(
                layout.SourcePathFor(descriptor.Version, descriptor.FileFor(SourceFileRole.SpecificationPdf)),
                Path.Combine(knowledgeDirectory, "spec"),
                layout.SpecExtractProjectDirectory,
                descriptor.Version,
                SpecificationDocument,
                cancellationToken);

            var guides = await this.GenerateGuidesAsync(layout, cancellationToken);
            var api = GenerateApi(layout);

            KnowledgeFileWriter.WriteText(
                Path.Combine(knowledgeDirectory, "datapackage.json"),
                DataPackageGenerator.Generate(knowledgeDirectory, descriptor));

            new InstalledVersionsStore(layout.KnowledgeRoot).MarkGenerated(descriptor.Version, spec.Succeeded);

            return new GenerateOutcome(metamodel, spec, guides, api);
        }

        private async Task<IReadOnlyList<GuideGenerationOutcome>> GenerateGuidesAsync(RepositoryLayout layout, CancellationToken cancellationToken)
        {
            var store = new InstalledVersionsStore(layout.KnowledgeRoot);
            var outcomes = new List<GuideGenerationOutcome>();

            foreach (var guide in store.Load().Guides.Where(g => g.Imported))
            {
                var pdfPath = GuideImporter.ImportedPdfPathFor(layout, guide.Version);
                var outcome = pdfPath is null
                    ? SpecExtractionOutcome.Skipped($"The imported implementation guide {guide.Version} is no longer in {layout.GuideSourcesDirectoryFor(guide.Version)} - run 'import-guide' again.")
                    : await this.specExtractRunner.ExtractAsync(pdfPath, layout.GuideKnowledgeDirectoryFor(guide.Version), layout.SpecExtractProjectDirectory, guide.Version, GuideDocument, cancellationToken);

                store.MarkGuideGenerated(guide.Version, outcome.Succeeded);
                outcomes.Add(new GuideGenerationOutcome(guide.Version, outcome));
            }

            return outcomes;
        }

        private static ApiGenerationOutcome GenerateApi(RepositoryLayout layout)
        {
            var reqifSharpRoot = Path.Combine(layout.KnowledgeRoot, "reqifsharp");
            var staging = Path.Combine(reqifSharpRoot, ".staging");

            try
            {
                if (Directory.Exists(staging))
                {
                    Directory.Delete(staging, recursive: true);
                }

                var result = ApiGenerator.Generate(staging);
                var target = layout.ApiKnowledgeDirectoryFor(result.ReqIFSharpVersion);
                if (Directory.Exists(target))
                {
                    Directory.Delete(target, recursive: true);
                }

                Directory.Move(staging, target);

                new InstalledVersionsStore(layout.KnowledgeRoot).MarkApiGenerated(new InstalledApi(result.ReqIFSharpVersion, result.ExtensionsVersion ?? "unknown"));
                return new ApiGenerationOutcome(true, result.ReqIFSharpVersion, result.TypeCount, result.Warnings, null);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                return new ApiGenerationOutcome(false, null, 0, [], $"The reqifsharp API reference could not be generated: {exception.Message}");
            }
        }
    }

    /// <summary>
    /// The result of <see cref="KnowledgeGenerationService.GenerateAsync"/>.
    /// </summary>
    /// <param name="Metamodel">The metamodel and schema generation result.</param>
    /// <param name="Specification">The specification text extraction outcome.</param>
    /// <param name="Guides">The extraction outcome of every imported implementation guide.</param>
    /// <param name="Api">The reqifsharp API reference outcome.</param>
    public sealed record GenerateOutcome(MetamodelGenerationResult Metamodel, SpecExtractionOutcome Specification, IReadOnlyList<GuideGenerationOutcome> Guides, ApiGenerationOutcome Api);

    /// <summary>
    /// The extraction outcome of one implementation guide.
    /// </summary>
    /// <param name="Version">The guide version.</param>
    /// <param name="Extraction">The extraction outcome.</param>
    public sealed record GuideGenerationOutcome(string Version, SpecExtractionOutcome Extraction);

    /// <summary>
    /// The outcome of generating the reqifsharp API reference.
    /// </summary>
    /// <param name="Succeeded">Whether it was generated.</param>
    /// <param name="ReqIFSharpVersion">The ReqIFSharp version described.</param>
    /// <param name="TypeCount">The number of type pages.</param>
    /// <param name="Warnings">Warnings reported by the generator.</param>
    /// <param name="SkippedReason">Why it was not generated, when it failed.</param>
    public sealed record ApiGenerationOutcome(bool Succeeded, string? ReqIFSharpVersion, int TypeCount, IReadOnlyList<string> Warnings, string? SkippedReason);
}
