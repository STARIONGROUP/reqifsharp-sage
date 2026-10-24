// -------------------------------------------------------------------------------------------------
// <copyright file="SystemAttributeConventions.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The system attribute names (ReqIF.*) of the prostep ivip ReqIF Implementation Guide 1.7 §2.3, per element kind,
    /// with the attribute definition kind each one should have. Used by rules G-003 to G-007.
    /// </summary>
    public static class SystemAttributeConventions
    {
        /// <summary>
        /// The element kind of attributes defined by a SPEC-OBJECT-TYPE.
        /// </summary>
        public const string SpecObject = "SpecObject";

        /// <summary>
        /// The element kind of attributes defined by a SPECIFICATION-TYPE.
        /// </summary>
        public const string Specification = "Specification";

        /// <summary>
        /// The element kind of attributes defined by a SPEC-RELATION-TYPE.
        /// </summary>
        public const string SpecRelation = "SpecRelation";

        /// <summary>
        /// Gets every convention.
        /// </summary>
        public static IReadOnlyList<SystemAttributeConvention> All { get; } =
        [
            new("ReqIF.ForeignID", SpecObject, "STRING"),
            new("ReqIF.ForeignCreatedBy", SpecObject, "STRING"),
            new("ReqIF.ForeignCreatedOn", SpecObject, "DATE"),
            new("ReqIF.ForeignCreatedThru", SpecObject, "ENUMERATION"),
            new("ReqIF.ForeignModifiedBy", SpecObject, "STRING"),
            new("ReqIF.ChapterName", SpecObject, "XHTML"),
            new("ReqIF.ChapterNumber", SpecObject, "STRING"),
            new("ReqIF.Name", SpecObject, "XHTML"),
            new("ReqIF.Text", SpecObject, "XHTML"),
            new("ReqIF.ForeignDeleted", SpecObject, "BOOLEAN"),
            new("ReqIF.AssociatedFiles", SpecObject, "STRING"),
            new("ReqIF.Category", SpecObject, "ENUMERATION"),
            new("ReqIF.ForeignRevision", SpecObject, "STRING"),
            new("ReqIF.ChangeDescription", SpecObject, "STRING"),
            new("ReqIF.FitCriteria", SpecObject, "STRING"),
            new("ReqIF.Description", SpecObject, "XHTML"),

            new("ReqIF.Name", Specification, "XHTML"),
            new("ReqIF.Prefix", Specification, "STRING"),
            new("ReqIF.ForeignCreatedBy", Specification, "STRING"),
            new("ReqIF.ForeignCreatedOn", Specification, "DATE"),
            new("ReqIF.ForeignModifiedBy", Specification, "STRING"),
            new("ReqIF.ForeignState", Specification, "ENUMERATION"),
            new("ReqIF.Project", Specification, "STRING"),
            new("ReqIF.ForeignID", Specification, "STRING"),
            new("ReqIF.Description", Specification, "XHTML"),
            new("ReqIF.ForeignRevision", Specification, "STRING"),
            new("ReqIF.ChangeDescription", Specification, "STRING"),
            new("ReqIF.ForeignBaseline", Specification, "STRING"),

            new("ReqIF.Name", SpecRelation, "XHTML"),
            new("ReqIF.ForeignCreatedBy", SpecRelation, "STRING"),
            new("ReqIF.ForeignCreatedOn", SpecRelation, "DATE"),
            new("ReqIF.ForeignModifiedBy", SpecRelation, "STRING"),
            new("ReqIF.ForeignID", SpecRelation, "STRING"),
            new("ReqIF.Description", SpecRelation, "XHTML"),
        ];

        /// <summary>
        /// Gets the distinct system attribute names.
        /// </summary>
        public static IReadOnlyList<string> Names { get; } = All.Select(convention => convention.Name).Distinct(StringComparer.Ordinal).ToList();

        /// <summary>
        /// Finds the system attribute name that equals <paramref name="name"/> ignoring letter case.
        /// </summary>
        /// <param name="name">An attribute definition LONG-NAME.</param>
        /// <returns>The case-correct system attribute name, or <c>null</c> when there is none.</returns>
        public static string? FindName(string name) => Names.FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Finds the convention for a system attribute name on an element kind.
        /// </summary>
        /// <param name="name">The case-correct system attribute name.</param>
        /// <param name="elementKind">"SpecObject", "Specification" or "SpecRelation".</param>
        /// <returns>The convention, or <c>null</c> when the guide does not define the name for that element kind.</returns>
        public static SystemAttributeConvention? Find(string name, string elementKind) =>
            All.FirstOrDefault(convention => convention.Name == name && convention.ElementKind == elementKind);
    }
}
