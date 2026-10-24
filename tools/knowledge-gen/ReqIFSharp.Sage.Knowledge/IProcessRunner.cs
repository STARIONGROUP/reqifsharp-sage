// -------------------------------------------------------------------------------------------------
// <copyright file="IProcessRunner.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Knowledge
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The result of running an external process.
    /// </summary>
    public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError);

    /// <summary>
    /// Abstracts running an external process, so the one place C# shells out to Python (the <c>spec</c>
    /// generation step) can be tested without actually invoking Python.
    /// </summary>
    public interface IProcessRunner
    {
        /// <summary>
        /// Runs <paramref name="fileName"/> with <paramref name="arguments"/> and returns its result.
        /// </summary>
        /// <exception cref="System.ComponentModel.Win32Exception">
        /// Thrown when <paramref name="fileName"/> cannot be found/started.
        /// </exception>
        Task<ProcessRunResult> RunAsync(string fileName, IReadOnlyList<string> arguments, string? workingDirectory = null, CancellationToken cancellationToken = default);
    }
}
