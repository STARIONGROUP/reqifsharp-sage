// -------------------------------------------------------------------------------------------------
// <copyright file="FakeProcessRunner.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge.Tests
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A hand-rolled <see cref="IProcessRunner"/> test double: no real Python/process invocation in unit tests.
    /// </summary>
    public sealed class FakeProcessRunner : IProcessRunner
    {
        private readonly ProcessRunResult? result;
        private readonly bool throwNotFound;
        private readonly string? onlyRespondsToFileName;
        private readonly ProcessRunResult? resultForOnlyRespondsToFileName;

        public IReadOnlyList<string>? LastArguments { get; private set; }

        public string? LastFileName { get; private set; }

        private FakeProcessRunner(ProcessRunResult? result, bool throwNotFound, string? onlyRespondsToFileName = null, ProcessRunResult? resultForOnlyRespondsToFileName = null)
        {
            this.result = result;
            this.throwNotFound = throwNotFound;
            this.onlyRespondsToFileName = onlyRespondsToFileName;
            this.resultForOnlyRespondsToFileName = resultForOnlyRespondsToFileName;
        }

        public static FakeProcessRunner Returning(ProcessRunResult result) => new(result, false);

        public static FakeProcessRunner NotFound() => new(null, true);

        /// <summary>
        /// Throws "not found" for every <paramref name="fileName"/> except <paramref name="respondingFileName"/>,
        /// which returns <paramref name="result"/> - simulates no Python being on <c>PATH</c> while a
        /// specific (e.g. provisioned <c>uv</c>) executable is runnable.
        /// </summary>
        public static FakeProcessRunner NotFoundExceptFor(string respondingFileName, ProcessRunResult result) => new(result, true, respondingFileName);

        /// <summary>
        /// Returns <paramref name="defaultResult"/> for every <paramref name="fileName"/> except
        /// <paramref name="specialFileName"/>, which returns <paramref name="specialResult"/> - simulates a
        /// plain Python candidate failing one way (e.g. a missing dependency) while a provisioned <c>uv</c>
        /// succeeds (or fails differently).
        /// </summary>
        public static FakeProcessRunner RespondingDifferentlyFor(string specialFileName, ProcessRunResult specialResult, ProcessRunResult defaultResult) =>
            new(defaultResult, false, specialFileName, specialResult);

        public Task<ProcessRunResult> RunAsync(string fileName, IReadOnlyList<string> arguments, string? workingDirectory = null, CancellationToken cancellationToken = default)
        {
            this.LastFileName = fileName;
            this.LastArguments = arguments;

            if (this.throwNotFound && fileName != this.onlyRespondsToFileName)
            {
                throw new Win32Exception("The system cannot find the file specified.");
            }

            if (fileName == this.onlyRespondsToFileName && this.resultForOnlyRespondsToFileName is not null)
            {
                return Task.FromResult(this.resultForOnlyRespondsToFileName);
            }

            return Task.FromResult(this.result!);
        }
    }
}
