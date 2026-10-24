// -------------------------------------------------------------------------------------------------
// <copyright file="ApiModel.cs" company="Starion Group S.A.">
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
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The reflected API of the documented packages.
    /// </summary>
    /// <param name="Packages">The documented packages, in the order they were given.</param>
    /// <param name="Types">The public types, ordered ordinally by <see cref="ApiTypeModel.FullName"/>.</param>
    /// <param name="Warnings">Problems met while building the model (missing documentation files, naming mismatches).</param>
    public sealed record ApiModel(
        IReadOnlyList<ApiPackageModel> Packages,
        IReadOnlyList<ApiTypeModel> Types,
        IReadOnlyList<string> Warnings)
    {
        /// <summary>
        /// Finds the documented type model of <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type (a constructed generic type is reduced to its definition).</param>
        /// <returns>The model, or <see langword="null"/> when the type is not documented.</returns>
        public ApiTypeModel? Find(Type? type)
        {
            if (type is null)
            {
                return null;
            }

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                type = type.GetGenericTypeDefinition();
            }

            foreach (var model in this.Types)
            {
                if (model.Type == type)
                {
                    return model;
                }
            }

            return null;
        }
    }
}
