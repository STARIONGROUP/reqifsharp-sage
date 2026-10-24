// -------------------------------------------------------------------------------------------------
// <copyright file="PythonSpecExtractRunner.cs" company="Starion Group S.A.">
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
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using ReqIFSharp.Sage.Knowledge.Toolchain;

    /// <summary>
    /// Invokes the <c>tools/spec-extract</c> Python package as a subprocess. This is the one place C#
    /// and Python meet in this repository: everything else is C# (see the root CLAUDE.md).
    /// </summary>
    public sealed class PythonSpecExtractRunner
    {
        private readonly IProcessRunner processRunner;
        private readonly IUvProvisioner uvProvisioner;

        /// <summary>
        /// Initializes a new instance of the <see cref="PythonSpecExtractRunner"/> class.
        /// </summary>
        /// <param name="processRunner">Runs the Python (or <c>uv</c>) subprocess.</param>
        /// <param name="uvProvisioner">
        /// Provisions <c>uv</c> as a last-resort fallback when no <c>.venv</c> or system Python is
        /// found - see <see cref="ExtractAsync"/>.
        /// </param>
        public PythonSpecExtractRunner(IProcessRunner processRunner, IUvProvisioner uvProvisioner)
        {
            this.processRunner = processRunner;
            this.uvProvisioner = uvProvisioner;
        }

        /// <summary>
        /// Runs <c>python -m spec_extract extract</c> against <paramref name="pdfPath"/>, writing clause
        /// markdown and indexes into <paramref name="outputDirectory"/>. Never throws: any failure
        /// (no usable Python or <c>uv</c>, the <c>spec_extract</c> package not installed, a non-zero
        /// exit code) results in a <see cref="SpecExtractionOutcome.Skipped"/> outcome with a
        /// human-readable reason.
        /// </summary>
        /// <param name="pdfPath">
        /// The locally fetched specification PDF (e.g. <c>sources/1.2/specs/ReqIF-1.2.pdf</c>).
        /// </param>
        /// <param name="outputDirectory">
        /// The <c>spec/</c> output directory (e.g. <c>knowledge/1.2/spec</c>).
        /// </param>
        /// <param name="specExtractProjectDirectory">
        /// The <c>tools/spec-extract</c> directory. A <c>.venv</c> under it, or a system Python
        /// interpreter, is tried first; if neither is found, <c>uv</c> is provisioned on demand and used
        /// to run <c>spec_extract</c> instead - it resolves a matching Python and installs
        /// <c>spec_extract</c>'s dependencies into a managed venv itself, so an installed plugin needs
        /// neither a pre-existing Python nor a provisioned <c>.venv</c>.
        /// </param>
        /// <param name="version">
        /// The specification version recorded in each clause's front matter.
        /// </param>
        /// <param name="document">
        /// The document name recorded in each clause's front matter and passed as <c>--document</c>
        /// (<c>"ReqIF"</c> for the OMG specification or <c>"PSI-Guide"</c> for the prostep implementation guide) - distinguishes which OMG specification's clauses these are,
        /// since more than one may be extracted into sibling knowledge trees.
        /// </param>
        public async Task<SpecExtractionOutcome> ExtractAsync(string pdfPath, string outputDirectory, string specExtractProjectDirectory, string version, string document, CancellationToken cancellationToken = default)
        {
            if (!File.Exists(pdfPath))
            {
                return SpecExtractionOutcome.Skipped($"The specification PDF was not found at {pdfPath} - run 'fetch' without --no-specs first.");
            }

            var srcDirectory = Path.Combine(specExtractProjectDirectory, "src");
            if (!Directory.Exists(srcDirectory))
            {
                return SpecExtractionOutcome.Skipped($"The spec-extract project was not found at {specExtractProjectDirectory} - this repository checkout may be incomplete.");
            }

            var extractArguments = new[] { "-m", "spec_extract", "extract", "--pdf", pdfPath, "--out", outputDirectory, "--document", document, "--version", version };

            var missingDependencies = false;

            foreach (var pythonExecutable in PythonCandidates(specExtractProjectDirectory))
            {
                try
                {
                    var result = await this.processRunner.RunAsync(pythonExecutable, extractArguments, workingDirectory: srcDirectory, cancellationToken);

                    if (result.ExitCode == 0)
                    {
                        return ValidateExtractedIndex(outputDirectory);
                    }

                    if (IsMissingInterpreterStub(result))
                    {
                        // The Windows "App Execution Alias" stub for the Microsoft Store: it starts, prints
                        // "Python was not found" and exits 9009. There is no interpreter here - try the
                        // next candidate (and then uv) exactly as if the executable did not exist.
                        continue;
                    }

                    if (IsMissingDependencyFailure(result.StandardError))
                    {
                        // This interpreter is missing spec_extract's own dependencies (pdfplumber) or the
                        // spec_extract package itself - not installed system-wide, and not what this
                        // candidate was for. Don't give up: uv provisions its own managed venv with these
                        // dependencies installed automatically, so try that before reporting failure.
                        missingDependencies = true;
                        continue;
                    }

                    return SpecExtractionOutcome.Skipped($"spec_extract exited with code {result.ExitCode}: {result.StandardError}{NetEffectSuffix}");
                }
                catch (Win32Exception)
                {
                    // This candidate interpreter isn't available - try the next one.
                }
            }

            var uvExecutable = await this.uvProvisioner.EnsureAsync(cancellationToken);
            if (uvExecutable is null)
            {
                return missingDependencies
                    ? SpecExtractionOutcome.Skipped(
                        "pdfplumber (spec_extract's Python dependency) is not installed for the Python " +
                        "interpreter found, and uv could not be provisioned to install it automatically " +
                        "(offline, or an unsupported platform). Install it yourself with 'pip install -e " +
                        "tools/spec-extract' from the repository root, then re-run 'generate'." + NetEffectSuffix)
                    : SpecExtractionOutcome.Skipped(
                        "No Python interpreter was found and uv could not be provisioned (offline, or an " +
                        "unsupported platform). Install Python 3.12+, or ensure uv can be provisioned - " +
                        "see tools/spec-extract/README.md." + NetEffectSuffix);
            }

            try
            {
                var uvArguments = new[] { "run", "--project", specExtractProjectDirectory, "python" }.Concat(extractArguments).ToArray();
                var uvResult = await this.processRunner.RunAsync(uvExecutable.FullName, uvArguments, workingDirectory: specExtractProjectDirectory, cancellationToken);

                if (uvResult.ExitCode == 0)
                {
                    return ValidateExtractedIndex(outputDirectory);
                }

                return missingDependencies
                    ? SpecExtractionOutcome.Skipped(
                        $"pdfplumber (spec_extract's Python dependency) is not installed for the Python " +
                        $"interpreter found, and the uv-provisioned run also failed (exit code " +
                        $"{uvResult.ExitCode}: {uvResult.StandardError}). Install it yourself with 'pip " +
                        $"install -e tools/spec-extract' from the repository root, then re-run 'generate'." + NetEffectSuffix)
                    : SpecExtractionOutcome.Skipped($"spec_extract (via uv) exited with code {uvResult.ExitCode}: {uvResult.StandardError}{NetEffectSuffix}");
            }
            catch (Win32Exception)
            {
                return SpecExtractionOutcome.Skipped(
                    $"The provisioned uv executable at {uvExecutable.FullName} could not be started.{NetEffectSuffix}");
            }
        }

        /// <summary>
        /// True when <paramref name="result"/> came from the Windows Store <c>python</c>/<c>python3</c>
        /// alias stub rather than a real interpreter: exit code 9009 (cmd's "command not found") or its
        /// "Python was not found" message.
        /// </summary>
        private static bool IsMissingInterpreterStub(ProcessRunResult result)
        {
            return result.ExitCode == 9009
                || result.StandardError.Contains("Python was not found", StringComparison.Ordinal)
                || result.StandardOutput.Contains("Python was not found", StringComparison.Ordinal);
        }

        /// <summary>
        /// True when <paramref name="standardError"/> looks like a Python "ModuleNotFoundError" - the
        /// signature of an interpreter that simply doesn't have <c>spec_extract</c> (or its own
        /// <c>pdfplumber</c> dependency) installed, as opposed to a genuine extraction failure (a malformed
        /// PDF, an unexpected document layout) that retrying under a different interpreter wouldn't fix.
        /// </summary>
        private static bool IsMissingDependencyFailure(string standardError)
        {
            return standardError.Contains("ModuleNotFoundError", StringComparison.Ordinal);
        }

        /// <summary>
        /// Appended to every skipped reason so it's never ambiguous what still works: spec extraction is
        /// wholly independent of the metamodel, schema and API generation (see
        /// <see cref="KnowledgeGenerationService.GenerateAsync"/>, which always runs those first).
        /// </summary>
        private const string NetEffectSuffix =
            " Metamodel, schema and API lookups are unaffected; only verbatim specification/guide " +
            "citation is unavailable until this is resolved.";

        /// <summary>
        /// Confirms <c>spec_extract</c> actually produced usable clauses rather than merely exiting 0 -
        /// a run that silently extracted zero clauses (e.g. because OMG served an HTML error page instead
        /// of the PDF, or a heuristic misfired on an unfamiliar document layout) would otherwise be recorded
        /// as a success, and every downstream reader would trust an empty index.
        /// </summary>
        private static SpecExtractionOutcome ValidateExtractedIndex(string outputDirectory)
        {
            var indexPath = Path.Combine(outputDirectory, "index.json");
            if (!File.Exists(indexPath))
            {
                return SpecExtractionOutcome.Skipped($"spec_extract exited successfully but did not write {indexPath}.");
            }

            int clauseCount;
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(indexPath));
                clauseCount = document.RootElement.GetArrayLength();
            }
            catch (JsonException exception)
            {
                return SpecExtractionOutcome.Skipped($"spec_extract exited successfully but {indexPath} could not be parsed: {exception.Message}");
            }

            return clauseCount > 0
                ? SpecExtractionOutcome.Ok()
                : SpecExtractionOutcome.Skipped($"spec_extract exited successfully but extracted 0 clauses from {indexPath} - the PDF may be malformed or the extraction heuristics may not fit this document's layout.");
        }

        private static string[] PythonCandidates(string specExtractProjectDirectory)
        {
            var venvPython = OperatingSystem.IsWindows()
                ? Path.Combine(specExtractProjectDirectory, ".venv", "Scripts", "python.exe")
                : Path.Combine(specExtractProjectDirectory, ".venv", "bin", "python");

            if (File.Exists(venvPython))
            {
                return [venvPython];
            }

            // On Windows, "python3" commonly resolves to a Microsoft Store app-execution-alias stub
            // (WindowsApps\python3.exe) even when a real interpreter is installed as "python.exe" - so
            // "python" is tried first there. Elsewhere "python3" is the more reliable name.
            return OperatingSystem.IsWindows() ? ["python", "python3"] : ["python3", "python"];
        }
    }
}
