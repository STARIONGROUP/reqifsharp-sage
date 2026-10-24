// -------------------------------------------------------------------------------------------------
// <copyright file="ApiAssemblySource.cs" company="Starion Group S.A.">
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
    /// One assembly to document, with the NuGet package it ships in and its XML documentation file.
    /// </summary>
    /// <param name="Assembly">The loaded assembly.</param>
    /// <param name="PackageId">The NuGet package id, e.g. <c>ReqIFSharp</c>.</param>
    /// <param name="XmlDocumentationPath">The path of the compiler-generated XML documentation file, or
    /// <see langword="null"/> to generate without documentation.</param>
    public sealed record ApiAssemblySource(Assembly Assembly, string PackageId, string? XmlDocumentationPath);
}
