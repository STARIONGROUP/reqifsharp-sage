// -------------------------------------------------------------------------------------------------
// <copyright file="FakeUvProvisioner.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.Knowledge.Tests
{
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using ReqIFSharp.Sage.Knowledge.Toolchain;

    /// <summary>
    /// A hand-rolled <see cref="IUvProvisioner"/> test double: no real download/cache lookup in unit tests.
    /// </summary>
    public sealed class FakeUvProvisioner : IUvProvisioner
    {
        private readonly FileInfo? executable;

        private FakeUvProvisioner(FileInfo? executable)
        {
            this.executable = executable;
        }

        public static FakeUvProvisioner Unavailable() => new(null);

        public static FakeUvProvisioner Returning(FileInfo executable) => new(executable);

        public Task<FileInfo?> EnsureAsync(CancellationToken cancellationToken = default) => Task.FromResult(this.executable);
    }
}
