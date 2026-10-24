// -------------------------------------------------------------------------------------------------
// <copyright file="ApiEventModel.cs" company="Starion Group S.A.">
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
    /// A reflected public event.
    /// </summary>
    /// <param name="Event">The reflected event.</param>
    /// <param name="Name">The event name.</param>
    /// <param name="Type">The C# spelling of the event handler type.</param>
    /// <param name="IsStatic">Whether the event is static.</param>
    /// <param name="DocId">The XML documentation id.</param>
    /// <param name="Doc">The resolved documentation.</param>
    public sealed record ApiEventModel(
        EventInfo Event,
        string Name,
        string Type,
        bool IsStatic,
        string DocId,
        DocComment Doc);
}
