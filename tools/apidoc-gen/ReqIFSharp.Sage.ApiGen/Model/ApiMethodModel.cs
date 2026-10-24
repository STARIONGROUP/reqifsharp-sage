// -------------------------------------------------------------------------------------------------
// <copyright file="ApiMethodModel.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Model
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    using ReqIFSharp.Sage.ApiGen.Documentation;

    /// <summary>
    /// A reflected public method or constructor.
    /// </summary>
    /// <param name="Method">The reflected method or constructor.</param>
    /// <param name="Name">The member name (<c>.ctor</c> for constructors).</param>
    /// <param name="Heading">The overload-distinguishing heading, e.g. <c>Deserialize(string, bool, ValidationEventHandler)</c>.</param>
    /// <param name="Signature">The full C# declaration.</param>
    /// <param name="ReturnType">The C# spelling of the return type (<see langword="null"/> for constructors).</param>
    /// <param name="IsStatic">Whether the method is static.</param>
    /// <param name="IsAbstract">Whether the method is abstract.</param>
    /// <param name="IsVirtual">Whether the method can be overridden.</param>
    /// <param name="IsExtension">Whether the method is an extension method.</param>
    /// <param name="ExtendedType">The type the extension method extends (its <c>this</c> parameter type), or <see langword="null"/>.</param>
    /// <param name="Parameters">The parameters, in declaration order.</param>
    /// <param name="TypeParameters">The generic type parameter names.</param>
    /// <param name="DocId">The XML documentation id.</param>
    /// <param name="Doc">The resolved documentation.</param>
    public sealed record ApiMethodModel(
        MethodBase Method,
        string Name,
        string Heading,
        string Signature,
        string? ReturnType,
        bool IsStatic,
        bool IsAbstract,
        bool IsVirtual,
        bool IsExtension,
        Type? ExtendedType,
        IReadOnlyList<ApiParameterModel> Parameters,
        IReadOnlyList<string> TypeParameters,
        string DocId,
        DocComment Doc);
}
