// -------------------------------------------------------------------------------------------------
// <copyright file="ApiPackageModel.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Model
{
    using System.Reflection;

    /// <summary>
    /// A documented package: its assembly and versions.
    /// </summary>
    /// <param name="Assembly">The loaded assembly.</param>
    /// <param name="PackageId">The NuGet package id.</param>
    /// <param name="Version">The package version (informational version without any <c>+commit</c> suffix, or the file version).</param>
    /// <param name="AssemblyVersion">The assembly version.</param>
    public sealed record ApiPackageModel(Assembly Assembly, string PackageId, string Version, string AssemblyVersion);
}
