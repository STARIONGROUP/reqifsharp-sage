// -------------------------------------------------------------------------------------------------
// <copyright file="ApiModel.cs" company="Starion Group S.A.">
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
