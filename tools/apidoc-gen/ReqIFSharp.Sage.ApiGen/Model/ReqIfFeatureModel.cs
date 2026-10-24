// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIfFeatureModel.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Model
{
    using System.Globalization;

    /// <summary>
    /// The ReqIF metamodel feature a C# property implements, read from the library's <c>ReqIfProperty</c>
    /// attribute. The attribute carries no feature name, so <see cref="Feature"/> is derived from the C#
    /// property name (see <see cref="ReqIfNames.FeatureName"/>).
    /// </summary>
    /// <param name="Feature">The ReqIF feature name, e.g. <c>longName</c>.</param>
    /// <param name="XmlName">The XML element or attribute name of the feature, e.g. <c>LONG-NAME</c>, or <see langword="null"/> when the schema declares no such name.</param>
    /// <param name="Aggregation">The aggregation kind (<c>None</c>, <c>Shared</c>, <c>Composite</c>).</param>
    /// <param name="LowerValue">The multiplicity lower bound.</param>
    /// <param name="UpperValue">The multiplicity upper bound (<see cref="int.MaxValue"/> or negative for unbounded).</param>
    /// <param name="IsOrdered">Whether the feature is ordered.</param>
    /// <param name="IsUnique">Whether the feature is unique.</param>
    /// <param name="IsReadOnly">Whether the feature is read-only.</param>
    /// <param name="IsDerived">Whether the feature is derived.</param>
    /// <param name="IsDerivedUnion">Whether the feature is a derived union.</param>
    /// <param name="DefaultValue">The default value, or <see langword="null"/>.</param>
    public sealed record ReqIfFeatureModel(
        string Feature,
        string? XmlName,
        string Aggregation,
        int LowerValue,
        int UpperValue,
        bool IsOrdered,
        bool IsUnique,
        bool IsReadOnly,
        bool IsDerived,
        bool IsDerivedUnion,
        string? DefaultValue)
    {
        /// <summary>
        /// Gets the UML-style multiplicity, e.g. <c>1</c>, <c>0..1</c>, <c>0..*</c>.
        /// </summary>
        public string Multiplicity
        {
            get
            {
                var upper = this.UpperValue is < 0 or int.MaxValue ? "*" : this.UpperValue.ToString(CultureInfo.InvariantCulture);
                var lower = this.LowerValue.ToString(CultureInfo.InvariantCulture);
                return lower == upper ? lower : lower + ".." + upper;
            }
        }
    }
}
