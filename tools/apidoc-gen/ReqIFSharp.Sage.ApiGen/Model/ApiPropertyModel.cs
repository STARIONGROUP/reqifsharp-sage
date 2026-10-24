// -------------------------------------------------------------------------------------------------
// <copyright file="ApiPropertyModel.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Model
{
    using System.Reflection;

    using ReqIFSharp.Sage.ApiGen.Documentation;

    /// <summary>
    /// A reflected public property.
    /// </summary>
    /// <param name="Property">The reflected property.</param>
    /// <param name="Name">The property name.</param>
    /// <param name="Type">The C# spelling of the property type.</param>
    /// <param name="Getter">The getter accessibility (<c>public</c>, <c>protected</c>, ...) or <see langword="null"/> when there is no getter.</param>
    /// <param name="Setter">The setter accessibility or <see langword="null"/> when there is no setter.</param>
    /// <param name="IsStatic">Whether the property is static.</param>
    /// <param name="IsAbstract">Whether the property is abstract.</param>
    /// <param name="ReqIf">The ReqIF feature mapping, or <see langword="null"/>.</param>
    /// <param name="DocId">The XML documentation id.</param>
    /// <param name="Doc">The resolved documentation.</param>
    public sealed record ApiPropertyModel(
        PropertyInfo Property,
        string Name,
        string Type,
        string? Getter,
        string? Setter,
        bool IsStatic,
        bool IsAbstract,
        ReqIfFeatureModel? ReqIf,
        string DocId,
        DocComment Doc);
}
