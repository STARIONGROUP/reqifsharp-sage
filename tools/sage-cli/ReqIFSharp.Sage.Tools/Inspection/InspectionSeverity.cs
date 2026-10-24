// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionSeverity.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    /// <summary>
    /// The severity strings used in <see cref="InspectionFinding.Severity"/> and <see cref="InspectionRule.Severity"/>.
    /// </summary>
    public static class InspectionSeverity
    {
        /// <summary>
        /// The document violates the ReqIF standard or cannot be read.
        /// </summary>
        public const string Error = "error";

        /// <summary>
        /// The document is probably wrong, or deviates from a recommendation, but can be processed.
        /// </summary>
        public const string Warning = "warning";

        /// <summary>
        /// A noteworthy observation that is not a defect.
        /// </summary>
        public const string Info = "info";
    }
}
