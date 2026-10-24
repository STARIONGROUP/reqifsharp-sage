// -------------------------------------------------------------------------------------------------
// <copyright file="ApiPackageModel.cs" company="Starion Group S.A.">
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
