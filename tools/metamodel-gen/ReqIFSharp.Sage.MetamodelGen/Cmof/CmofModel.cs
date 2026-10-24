// -------------------------------------------------------------------------------------------------
// <copyright file="CmofModel.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.MetamodelGen.Cmof
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The in-memory form of a CMOF 2.0 metamodel file (such as OMG's <c>reqif.cmof</c>): its classes,
    /// primitive types and associations, with id-based lookups.
    /// </summary>
    public sealed class CmofModel
    {
        private readonly Dictionary<string, CmofClass> classesById;

        private readonly Dictionary<string, CmofProperty> propertiesById;

        private readonly Dictionary<string, CmofAssociation> associationsById;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmofModel"/> class.
        /// </summary>
        /// <param name="name">The name of the root package.</param>
        /// <param name="classes">Every class, in any package.</param>
        /// <param name="primitiveTypes">Every primitive type declared by the model itself.</param>
        /// <param name="associations">Every association.</param>
        public CmofModel(string name, IReadOnlyList<CmofClass> classes, IReadOnlyList<CmofPrimitiveType> primitiveTypes, IReadOnlyList<CmofAssociation> associations)
        {
            this.Name = name;
            this.Classes = classes.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
            this.PrimitiveTypes = primitiveTypes.OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
            this.Associations = associations;
            this.classesById = classes.ToDictionary(c => c.Id, StringComparer.Ordinal);
            this.associationsById = associations.ToDictionary(a => a.Id, StringComparer.Ordinal);
            this.propertiesById = classes.SelectMany(c => c.OwnedAttributes)
                .Concat(associations.SelectMany(a => a.OwnedEnds))
                .GroupBy(p => p.Id, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets the name of the root package (e.g. <c>ReqIF</c>).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets every class, sorted ordinally by name.
        /// </summary>
        public IReadOnlyList<CmofClass> Classes { get; }

        /// <summary>
        /// Gets every primitive type declared in the model, sorted ordinally by name.
        /// </summary>
        public IReadOnlyList<CmofPrimitiveType> PrimitiveTypes { get; }

        /// <summary>
        /// Gets every association, in document order.
        /// </summary>
        public IReadOnlyList<CmofAssociation> Associations { get; }

        /// <summary>
        /// Looks up a class by its <c>xmi:id</c>.
        /// </summary>
        /// <param name="id">The id to look up.</param>
        /// <returns>The class, or null when no class has that id.</returns>
        public CmofClass? FindClass(string? id)
        {
            return id is not null && this.classesById.TryGetValue(id, out var found) ? found : null;
        }

        /// <summary>
        /// Looks up a property (an owned attribute or an association-owned end) by its <c>xmi:id</c>.
        /// </summary>
        /// <param name="id">The id to look up.</param>
        /// <returns>The property, or null when no property has that id.</returns>
        public CmofProperty? FindProperty(string? id)
        {
            return id is not null && this.propertiesById.TryGetValue(id, out var found) ? found : null;
        }

        /// <summary>
        /// Gets the other end of <paramref name="property"/>'s binary association, if it has one.
        /// </summary>
        /// <param name="property">The property whose opposite end is wanted.</param>
        /// <returns>The opposite end, or null when the property is not an end of a binary association.</returns>
        public CmofProperty? OppositeOf(CmofProperty property)
        {
            if (property.AssociationId is null || !this.associationsById.TryGetValue(property.AssociationId, out var association))
            {
                return null;
            }

            if (association.MemberEndIds.Count != 2)
            {
                return null;
            }

            var otherId = association.MemberEndIds[0] == property.Id ? association.MemberEndIds[1] : association.MemberEndIds[0];
            return this.FindProperty(otherId);
        }
    }
}
