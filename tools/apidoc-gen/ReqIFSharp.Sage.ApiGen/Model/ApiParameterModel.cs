// -------------------------------------------------------------------------------------------------
// <copyright file="ApiParameterModel.cs" company="Starion Group S.A.">
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
    /// One parameter of a reflected method or constructor.
    /// </summary>
    /// <param name="Parameter">The reflected parameter.</param>
    /// <param name="Name">The parameter name.</param>
    /// <param name="Type">The C# spelling of the parameter type, e.g. <c>IEnumerable&lt;ReqIF&gt;</c>.</param>
    /// <param name="Modifier">The modifier (<c>this</c>, <c>ref</c>, <c>out</c>, <c>in</c>, <c>params</c>) or <see langword="null"/>.</param>
    /// <param name="IsOptional">Whether the parameter has a default value.</param>
    /// <param name="DefaultValue">The C# spelling of the default value, or <see langword="null"/>.</param>
    public sealed record ApiParameterModel(
        ParameterInfo Parameter,
        string Name,
        string Type,
        string? Modifier,
        bool IsOptional,
        string? DefaultValue);
}
