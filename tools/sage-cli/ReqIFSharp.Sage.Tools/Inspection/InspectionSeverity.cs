// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionSeverity.cs" company="Starion Group S.A.">
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
