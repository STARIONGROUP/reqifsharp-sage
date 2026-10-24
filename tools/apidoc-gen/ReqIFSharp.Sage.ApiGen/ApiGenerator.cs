// -------------------------------------------------------------------------------------------------
// <copyright file="ApiGenerator.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.ApiGen
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    using ReqIFSharp.Extensions.Services;
    using ReqIFSharp.Sage.ApiGen.Model;
    using ReqIFSharp.Sage.ApiGen.Rendering;

    /// <summary>
    /// Generates the "reqifsharp API" part of the knowledge base by reflecting in-process over the ReqIFSharp
    /// and ReqIFSharp.Extensions assemblies this generator references, together with the XML documentation
    /// files shipped next to them. Writes, into the output directory:
    /// <c>types/&lt;FullName&gt;.md</c> (one page per public type), <c>index.json</c>, <c>index.md</c>,
    /// <c>mapping.json</c> (ReqIF metaclass ↔ C# type ↔ XML element), <c>api.json</c> (full model) and
    /// <c>manifest.json</c>. Output is deterministic: ordinal ordering, LF line endings, UTF-8 without BOM,
    /// fixed JSON options, no timestamps or machine paths.
    /// </summary>
    public static class ApiGenerator
    {
        /// <summary>
        /// The NuGet package id of the core library.
        /// </summary>
        public const string ReqIFSharpPackageId = "ReqIFSharp";

        /// <summary>
        /// The NuGet package id of the extensions library.
        /// </summary>
        public const string ExtensionsPackageId = "ReqIFSharp.Extensions";

        /// <summary>
        /// The sub-directory of the output directory that holds the per-type pages.
        /// </summary>
        public const string TypesDirectoryName = "types";

        private static readonly UTF8Encoding Utf8WithoutByteOrderMark = new(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Generates the API knowledge of the ReqIFSharp and ReqIFSharp.Extensions assemblies referenced by this
        /// generator into <paramref name="outputDirectory"/>.
        /// </summary>
        /// <param name="outputDirectory">The directory to write into; created if missing. Its <c>types/</c>
        /// sub-directory is replaced.</param>
        /// <returns>The result.</returns>
        public static ApiGenerationResult Generate(string outputDirectory)
        {
            return Generate(outputDirectory, DefaultSources());
        }

        /// <summary>
        /// Generates the API knowledge of the given assemblies into <paramref name="outputDirectory"/>.
        /// </summary>
        /// <param name="outputDirectory">The directory to write into; created if missing. Its <c>types/</c>
        /// sub-directory is replaced.</param>
        /// <param name="sources">The assemblies to document.</param>
        /// <returns>The result.</returns>
        public static ApiGenerationResult Generate(string outputDirectory, IReadOnlyList<ApiAssemblySource> sources)
        {
            var model = ApiModelBuilder.Build(sources);
            var files = Render(model);

            Directory.CreateDirectory(outputDirectory);
            var typesDirectory = Path.Combine(outputDirectory, TypesDirectoryName);
            if (Directory.Exists(typesDirectory))
            {
                Directory.Delete(typesDirectory, recursive: true);
            }

            Directory.CreateDirectory(typesDirectory);

            foreach (var (relativePath, content) in files)
            {
                var path = Path.Combine(outputDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
                File.WriteAllText(path, content.Replace("\r\n", "\n"), Utf8WithoutByteOrderMark);
            }

            var reqIfSharp = model.Packages.FirstOrDefault(package => package.PackageId == ReqIFSharpPackageId);
            var extensions = model.Packages.FirstOrDefault(package => package.PackageId == ExtensionsPackageId);

            return new ApiGenerationResult(
                model.Types.Count,
                reqIfSharp?.Version ?? string.Empty,
                extensions?.Version,
                outputDirectory,
                files.Count,
                model.Warnings);
        }

        /// <summary>
        /// Renders every output file of <paramref name="model"/> in memory, keyed by '/'-separated relative path
        /// and ordered ordinally by that path.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>The files.</returns>
        public static IReadOnlyList<(string RelativePath, string Content)> Render(ApiModel model)
        {
            var renderer = new TypePageRenderer(model);
            var files = new List<(string RelativePath, string Content)>
            {
                ("api.json", ApiJsonDocuments.ApiJson(model)),
                ("index.json", ApiJsonDocuments.IndexJson(model)),
                ("index.md", ApiJsonDocuments.IndexMarkdown(model)),
                ("manifest.json", ApiJsonDocuments.ManifestJson(model)),
                ("mapping.json", ApiJsonDocuments.MappingJson(model)),
            };

            files.AddRange(model.Types.Select(type => (TypesDirectoryName + "/" + TypePageRenderer.FileNameOf(type), renderer.Render(type))));
            files.Sort((left, right) => StringComparer.Ordinal.Compare(left.RelativePath, right.RelativePath));
            return files;
        }

        /// <summary>
        /// Gets the default sources: the ReqIFSharp and ReqIFSharp.Extensions assemblies loaded in this process,
        /// with the XML documentation files next to them (or next to this generator).
        /// </summary>
        /// <returns>The sources.</returns>
        public static IReadOnlyList<ApiAssemblySource> DefaultSources()
        {
            return new[]
            {
                SourceFor(typeof(ReqIF).Assembly, ReqIFSharpPackageId),
                SourceFor(typeof(ReqIFLoaderService).Assembly, ExtensionsPackageId),
            };
        }

        /// <summary>
        /// Creates the source of an assembly, locating its XML documentation file next to the assembly, else in the
        /// application base directory, else in the package's <c>lib/*</c> folder of the NuGet global packages folder.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <param name="packageId">The NuGet package id it ships in.</param>
        /// <returns>The source (with a <see langword="null"/> documentation path when no file is found).</returns>
        public static ApiAssemblySource SourceFor(Assembly assembly, string packageId)
        {
            var fileName = (assembly.GetName().Name ?? packageId) + ".xml";
            var candidates = new List<string>();

            if (!string.IsNullOrEmpty(assembly.Location))
            {
                candidates.Add(Path.Combine(Path.GetDirectoryName(assembly.Location) ?? string.Empty, fileName));
            }

            candidates.Add(Path.Combine(AppContext.BaseDirectory, fileName));

            // Fall back to the NuGet global packages folder, for hosts that copy the assemblies but not the
            // documentation files (CopyDocumentationFilesFromPackages is evaluated per project).
            var packagesRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (string.IsNullOrEmpty(packagesRoot))
            {
                packagesRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            }

            var version = ApiModelBuilder.PackageVersionOf(assembly);
            var libDirectory = Path.Combine(packagesRoot, packageId.ToLowerInvariant(), version.ToLowerInvariant(), "lib");
            if (Directory.Exists(libDirectory))
            {
                candidates.AddRange(Directory.GetDirectories(libDirectory)
                    .OrderBy(directory => directory, StringComparer.Ordinal)
                    .Select(directory => Path.Combine(directory, fileName)));
            }

            var found = candidates.FirstOrDefault(File.Exists);
            return new ApiAssemblySource(assembly, packageId, found);
        }
    }
}
