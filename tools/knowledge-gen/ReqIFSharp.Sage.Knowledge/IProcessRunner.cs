// -------------------------------------------------------------------------------------------------
// <copyright file="IProcessRunner.cs" company="Starion Group S.A.">
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
