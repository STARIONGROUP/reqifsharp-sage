// -------------------------------------------------------------------------------------------------
// <copyright file="IUvProvisioner.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Knowledge.Toolchain
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Ensures a cached, checksum-verified <c>uv</c> executable is available.
    /// </summary>
    /// <remarks>
    /// <c>uv</c> is the Python toolchain <see cref="PythonSpecExtractRunner"/> falls back to when no
    /// <c>.venv</c> or system Python is found: it resolves or fetches a matching Python itself and
    /// installs <c>tools/spec-extract</c>'s dependencies into a managed venv on demand, so an installed
    /// plugin never needs a pre-existing Python or a provisioned <c>.venv</c> for verbatim spec
    /// citation to work.
    /// </remarks>
    public interface IUvProvisioner
    {
        /// <summary>Ensures <c>uv</c> is cached, downloading and verifying it if necessary.</summary>
        /// <returns>The cached executable, or <c>null</c> when it could not be provisioned.</returns>
        Task<FileInfo?> EnsureAsync(CancellationToken cancellationToken = default);
    }
}
