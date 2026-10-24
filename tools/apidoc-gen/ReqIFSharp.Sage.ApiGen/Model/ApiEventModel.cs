// -------------------------------------------------------------------------------------------------
// <copyright file="ApiEventModel.cs" company="Starion Group S.A.">
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
