// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionOptions.cs" company="Starion Group S.A.">
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
    /// Options for <see cref="ReqIFInspector.Inspect"/>.
    /// </summary>
    /// <param name="IncludeGuideRules">Whether to also run the prostep ivip ReqIF Implementation Guide rules (G-xxx).</param>
    /// <param name="GuideVersion">
    /// The Implementation Guide version to check against; <c>null</c> means the latest implemented version
    /// (<see cref="InspectionRuleCatalog.GuideVersion"/>). Any other version is reported with rule G-000 and the
    /// implemented version is applied.
    /// </param>
    public sealed record InspectionOptions(bool IncludeGuideRules, string? GuideVersion);
}
