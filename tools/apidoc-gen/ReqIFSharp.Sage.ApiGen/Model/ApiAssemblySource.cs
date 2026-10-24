// -------------------------------------------------------------------------------------------------
// <copyright file="ApiAssemblySource.cs" company="Starion Group S.A.">
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
    /// One assembly to document, with the NuGet package it ships in and its XML documentation file.
    /// </summary>
    /// <param name="Assembly">The loaded assembly.</param>
    /// <param name="PackageId">The NuGet package id, e.g. <c>ReqIFSharp</c>.</param>
    /// <param name="XmlDocumentationPath">The path of the compiler-generated XML documentation file, or
    /// <see langword="null"/> to generate without documentation.</param>
    public sealed record ApiAssemblySource(Assembly Assembly, string PackageId, string? XmlDocumentationPath);
}
