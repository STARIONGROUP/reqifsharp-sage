// -------------------------------------------------------------------------------------------------
// <copyright file="TestModel.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
