// -------------------------------------------------------------------------------------------------
// <copyright file="FakeUvProvisioner.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
