// -------------------------------------------------------------------------------------------------
// <copyright file="TestModel.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.ApiGen.Tests
{
    using System;
    using System.Linq;

    using ReqIFSharp.Sage.ApiGen;
    using ReqIFSharp.Sage.ApiGen.Model;

    /// <summary>
    /// The model of the real ReqIFSharp / ReqIFSharp.Extensions package assemblies, built once per test run.
    /// </summary>
    internal static class TestModel
    {
        private static readonly Lazy<ApiModel> Model = new(() => ApiModelBuilder.Build(ApiGenerator.DefaultSources()));

        /// <summary>
        /// Gets the shared model.
        /// </summary>
        public static ApiModel Instance => Model.Value;

        /// <summary>
        /// Gets the model of the type with the given full name.
        /// </summary>
        public static ApiTypeModel Type(string fullName)
        {
            return Instance.Types.Single(type => type.FullName == fullName);
        }
    }
}
