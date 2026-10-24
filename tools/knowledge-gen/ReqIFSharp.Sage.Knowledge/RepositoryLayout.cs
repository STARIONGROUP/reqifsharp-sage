// -------------------------------------------------------------------------------------------------
// <copyright file="RepositoryLayout.cs" company="Starion Group S.A.">
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
    using System;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The fixed directory layout of a <c>reqifsharp-sage</c> plugin checkout. Everything under
    /// <c>sources/&lt;...&gt;/</c> and <c>knowledge/&lt;...&gt;/</c> is git-ignored and generated per machine.
    /// </summary>
    public sealed partial class RepositoryLayout
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RepositoryLayout"/> class.
        /// </summary>
        /// <param name="repositoryRoot">The plugin repository root.</param>
        public RepositoryLayout(string repositoryRoot)
        {
            this.RepositoryRoot = repositoryRoot;
        }

        /// <summary>
        /// Checks that <paramref name="version"/> is a plain version name (letters, digits, '.', '-' and
        /// '_', starting with a letter or digit, no "..") so it can never turn a directory path into one
        /// outside the layout - versions come from the command line.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <returns>The version, unchanged.</returns>
        /// <exception cref="ArgumentException">The version is empty or contains other characters.</exception>
        public static string ValidateVersion(string version)
        {
            if (string.IsNullOrEmpty(version) || version.Length > 64 || version.Contains("..", StringComparison.Ordinal) || !VersionPattern().IsMatch(version))
            {
                throw new ArgumentException($"'{version}' is not a valid version: use letters, digits, '.', '-' and '_' only, e.g. 1.2.", nameof(version));
            }

            return version;
        }

        /// <summary>
        /// Gets the plugin repository's root directory.
        /// </summary>
        public string RepositoryRoot { get; }

        /// <summary>
        /// Gets <c>sources/</c>.
        /// </summary>
        public string SourcesRoot => Path.Combine(this.RepositoryRoot, "sources");

        /// <summary>
        /// Gets <c>knowledge/</c>, which also holds the local <c>installed.json</c>.
        /// </summary>
        public string KnowledgeRoot => Path.Combine(this.RepositoryRoot, "knowledge");

        /// <summary>
        /// Gets the <c>tools/spec-extract</c> Python project directory.
        /// </summary>
        public string SpecExtractProjectDirectory => Path.Combine(this.RepositoryRoot, "tools", "spec-extract");

        /// <summary>
        /// Gets <c>sources/&lt;version&gt;</c>: the OMG files of one ReqIF version.
        /// </summary>
        /// <param name="version">The ReqIF version.</param>
        /// <returns>The directory.</returns>
        public string SourcesDirectoryFor(string version) => Path.Combine(this.SourcesRoot, ValidateVersion(version));

        /// <summary>
        /// Gets the local path of one source file of a ReqIF version.
        /// </summary>
        /// <param name="version">The ReqIF version.</param>
        /// <param name="file">The source file.</param>
        /// <returns>The path.</returns>
        public string SourcePathFor(string version, SourceFile file) => Path.Combine(this.SourcesDirectoryFor(version), file.RelativePath.Replace('/', Path.DirectorySeparatorChar));

        /// <summary>
        /// Gets <c>knowledge/&lt;version&gt;</c>: the metamodel, schema and spec knowledge of one ReqIF version.
        /// </summary>
        /// <param name="version">The ReqIF version.</param>
        /// <returns>The directory.</returns>
        public string KnowledgeDirectoryFor(string version) => Path.Combine(this.KnowledgeRoot, ValidateVersion(version));

        /// <summary>
        /// Gets <c>sources/psi-guide/&lt;version&gt;</c>: the imported prostep implementation guide.
        /// </summary>
        /// <param name="version">The guide version.</param>
        /// <returns>The directory.</returns>
        public string GuideSourcesDirectoryFor(string version) => Path.Combine(this.SourcesRoot, "psi-guide", ValidateVersion(version));

        /// <summary>
        /// Gets <c>knowledge/psi-guide/&lt;version&gt;</c>: the extracted implementation guide clauses.
        /// </summary>
        /// <param name="version">The guide version.</param>
        /// <returns>The directory.</returns>
        public string GuideKnowledgeDirectoryFor(string version) => Path.Combine(this.KnowledgeRoot, "psi-guide", ValidateVersion(version));

        /// <summary>
        /// Gets <c>knowledge/reqifsharp/&lt;version&gt;</c>: the reqifsharp API reference for one package version.
        /// </summary>
        /// <param name="reqifSharpVersion">The ReqIFSharp package version.</param>
        /// <returns>The directory.</returns>
        public string ApiKnowledgeDirectoryFor(string reqifSharpVersion) => Path.Combine(this.KnowledgeRoot, "reqifsharp", ValidateVersion(reqifSharpVersion));

        [GeneratedRegex("^[0-9A-Za-z][0-9A-Za-z._-]*$")]
        private static partial Regex VersionPattern();
    }
}
