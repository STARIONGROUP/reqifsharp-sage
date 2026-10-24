// -------------------------------------------------------------------------------------------------
// <copyright file="ApiFieldModel.cs" company="Starion Group S.A.">
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
    /// A reflected public field, or an enumeration literal.
    /// </summary>
    /// <param name="Field">The reflected field.</param>
    /// <param name="Name">The field name.</param>
    /// <param name="Type">The C# spelling of the field type.</param>
    /// <param name="IsStatic">Whether the field is static.</param>
    /// <param name="IsConstant">Whether the field is a constant (<c>const</c> or enumeration literal).</param>
    /// <param name="IsReadOnly">Whether the field is <c>readonly</c>.</param>
    /// <param name="Value">The C# literal of a constant's value (the underlying number for enumeration literals), or <see langword="null"/>.</param>
    /// <param name="DocId">The XML documentation id.</param>
    /// <param name="Doc">The resolved documentation.</param>
    public sealed record ApiFieldModel(
        FieldInfo Field,
        string Name,
        string Type,
        bool IsStatic,
        bool IsConstant,
        bool IsReadOnly,
        string? Value,
        string DocId,
        DocComment Doc);
}
