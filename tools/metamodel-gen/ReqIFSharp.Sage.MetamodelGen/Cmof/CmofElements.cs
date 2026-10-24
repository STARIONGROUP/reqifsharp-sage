// -------------------------------------------------------------------------------------------------
// <copyright file="CmofElements.cs" company="Starion Group S.A.">
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
    using System.Collections.Generic;
    using System.Globalization;

    /// <summary>
    /// A <c>cmof:Class</c>.
    /// </summary>
    /// <param name="Id">The <c>xmi:id</c>.</param>
    /// <param name="Name">The class name.</param>
    /// <param name="Package">The qualified name of the owning package (e.g. <c>ReqIF</c> or <c>ReqIF::DataTypes</c>).</param>
    /// <param name="IsAbstract">Whether the class is abstract.</param>
    /// <param name="SuperClassIds">The ids of the direct superclasses.</param>
    /// <param name="OwnedAttributes">The owned attributes, in document order.</param>
    /// <param name="Documentation">The body of the class's owned comment, if any.</param>
    /// <param name="Tags">The <c>cmof:Tag</c> name/value pairs attached to the class.</param>
    public sealed record CmofClass(
        string Id,
        string Name,
        string Package,
        bool IsAbstract,
        IReadOnlyList<string> SuperClassIds,
        IReadOnlyList<CmofProperty> OwnedAttributes,
        string? Documentation,
        IReadOnlyDictionary<string, string> Tags)
    {
        /// <summary>
        /// Gets the qualified name, e.g. <c>ReqIF::SpecObject</c>.
        /// </summary>
        public string QualifiedName => $"{this.Package}::{this.Name}";
    }

    /// <summary>
    /// A <c>cmof:Property</c>: a class's owned attribute or an association's owned end.
    /// </summary>
    /// <param name="Id">The <c>xmi:id</c>.</param>
    /// <param name="Name">The property name.</param>
    /// <param name="OwnerId">The id of the owning class, or of the owning association for an association-owned end.</param>
    /// <param name="TypeId">The id of the type when it is declared in the model itself, otherwise null.</param>
    /// <param name="TypeName">The type name, resolved from <paramref name="TypeId"/> or taken from an external href fragment such as <c>#Boolean</c>.</param>
    /// <param name="Lower">The lower multiplicity bound (CMOF default 1).</param>
    /// <param name="Upper">The upper multiplicity bound, <c>*</c> for unbounded (CMOF default 1).</param>
    /// <param name="IsComposite">Whether the property is a composite (containment) end.</param>
    /// <param name="IsOrdered">Whether the values are ordered.</param>
    /// <param name="AssociationId">The id of the association this property is an end of, if any.</param>
    /// <param name="IsOwnedByAssociation">Whether the association owns this end (so it is not navigable from a class).</param>
    /// <param name="Documentation">The body of the property's owned comment, if any.</param>
    /// <param name="Tags">The <c>cmof:Tag</c> name/value pairs attached to the property.</param>
    public sealed record CmofProperty(
        string Id,
        string Name,
        string OwnerId,
        string? TypeId,
        string? TypeName,
        int Lower,
        string Upper,
        bool IsComposite,
        bool IsOrdered,
        string? AssociationId,
        bool IsOwnedByAssociation,
        string? Documentation,
        IReadOnlyDictionary<string, string> Tags)
    {
        /// <summary>
        /// Gets the multiplicity in UML notation, e.g. <c>[0..*]</c> or <c>[1]</c>.
        /// </summary>
        public string Multiplicity
        {
            get
            {
                var lower = this.Lower.ToString(CultureInfo.InvariantCulture);
                return lower == this.Upper ? $"[{lower}]" : $"[{lower}..{this.Upper}]";
            }
        }
    }

    /// <summary>
    /// A <c>cmof:PrimitiveType</c> declared in the model.
    /// </summary>
    /// <param name="Id">The <c>xmi:id</c>.</param>
    /// <param name="Name">The type name.</param>
    /// <param name="Package">The qualified name of the owning package.</param>
    /// <param name="Documentation">The body of its owned comment, if any.</param>
    public sealed record CmofPrimitiveType(string Id, string Name, string Package, string? Documentation)
    {
        /// <summary>
        /// Gets the qualified name, e.g. <c>ReqIF::DataTypes::DateTime</c>.
        /// </summary>
        public string QualifiedName => $"{this.Package}::{this.Name}";
    }

    /// <summary>
    /// A <c>cmof:Association</c>.
    /// </summary>
    /// <param name="Id">The <c>xmi:id</c>.</param>
    /// <param name="Name">The association name; usually null in the ReqIF model.</param>
    /// <param name="MemberEndIds">The ids of the member ends, in document order.</param>
    /// <param name="OwnedEnds">The ends owned by the association itself.</param>
    public sealed record CmofAssociation(string Id, string? Name, IReadOnlyList<string> MemberEndIds, IReadOnlyList<CmofProperty> OwnedEnds);
}
