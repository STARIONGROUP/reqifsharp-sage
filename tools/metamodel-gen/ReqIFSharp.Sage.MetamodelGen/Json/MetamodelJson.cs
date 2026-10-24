// -------------------------------------------------------------------------------------------------
// <copyright file="MetamodelJson.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen.Json
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ReqIFSharp.Sage.MetamodelGen.Cmof;

    /// <summary>
    /// Builds the <c>metamodel/metamodel.json</c> document and the <c>index.json</c> rows from a
    /// <see cref="MetamodelView"/>.
    /// </summary>
    public static class MetamodelJson
    {
        /// <summary>
        /// Builds the whole <c>metamodel.json</c> document.
        /// </summary>
        /// <param name="view">The joined metamodel and schema.</param>
        /// <returns>The document.</returns>
        public static MetamodelDocument Build(MetamodelView view)
        {
            var classes = view.Model.Classes.Select(c => new ClassNode(
                c.Name,
                c.QualifiedName,
                c.Package,
                c.IsAbstract,
                view.XmlElementOf(c),
                c.Documentation,
                view.DirectSuperClassesOf(c).Select(s => s.Name).ToList(),
                view.DirectSubclassesOf(c).Select(s => s.Name).ToList(),
                view.AllAncestorsOf(c).Select(s => s.Name).ToList(),
                view.AllDescendantsOf(c).Select(s => s.Name).ToList(),
                c.OwnedAttributes.OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => Feature(view, c, c, p)).ToList(),
                view.InheritedFeaturesOf(c).Select(f => Feature(view, c, f.Owner, f.Feature)).ToList()))
                .ToList();

            var primitiveTypes = view.Model.PrimitiveTypes
                .Select(p => new PrimitiveTypeNode(p.Name, p.QualifiedName, p.Package))
                .ToList();

            return new MetamodelDocument(view.Model.Name, classes, primitiveTypes);
        }

        /// <summary>
        /// Builds the <c>metamodel/index.json</c> rows: one per class and primitive type, sorted by name.
        /// </summary>
        /// <param name="view">The joined metamodel and schema.</param>
        /// <returns>The rows.</returns>
        public static IReadOnlyList<MetamodelIndexRow> BuildIndex(MetamodelView view)
        {
            return view.Model.Classes
                .Select(c => new MetamodelIndexRow(c.Name, "class", c.Package, c.QualifiedName, c.IsAbstract, view.XmlElementOf(c), $"elements/{c.Name}.md"))
                .Concat(view.Model.PrimitiveTypes.Select(p => new MetamodelIndexRow(p.Name, "primitiveType", p.Package, p.QualifiedName, false, null, $"elements/{p.Name}.md")))
                .OrderBy(r => r.Name, StringComparer.Ordinal)
                .ToList();
        }

        private static FeatureNode Feature(MetamodelView view, CmofClass viewedFrom, CmofClass owner, CmofProperty property)
        {
            var typeIsClass = view.Model.FindClass(property.TypeId) is not null;
            var binding = view.BindingOf(viewedFrom, property);
            var opposite = view.Model.OppositeOf(property);

            return new FeatureNode(
                property.Name,
                owner.Name,
                property.TypeName,
                typeIsClass ? "class" : "primitive",
                property.Lower,
                property.Upper,
                property.IsComposite,
                property.IsOrdered,
                !typeIsClass ? "value" : property.IsComposite ? "composite" : "reference",
                string.IsNullOrEmpty(opposite?.Name) ? null : opposite!.Name,
                property.Documentation,
                binding.Kind == "none" ? null : new XmlBindingNode(binding.Kind, binding.XmlName, binding.Type, binding.Use, binding.Occurs, binding.InnerOccurs, binding.Options.Select(o => o.Name).ToList()));
        }
    }

    /// <summary>
    /// The root of <c>metamodel.json</c>.
    /// </summary>
    /// <param name="Model">The root package name.</param>
    /// <param name="Classes">Every class, sorted by name.</param>
    /// <param name="PrimitiveTypes">Every primitive type, sorted by name.</param>
    public sealed record MetamodelDocument(string Model, IReadOnlyList<ClassNode> Classes, IReadOnlyList<PrimitiveTypeNode> PrimitiveTypes);

    /// <summary>
    /// One class in <c>metamodel.json</c>, with precomputed inheritance closures.
    /// </summary>
    /// <param name="Name">The class name.</param>
    /// <param name="QualifiedName">The qualified name.</param>
    /// <param name="Package">The owning package.</param>
    /// <param name="IsAbstract">Whether the class is abstract.</param>
    /// <param name="XmlElement">The XML element name, or null for an abstract class.</param>
    /// <param name="Documentation">The metamodel documentation.</param>
    /// <param name="DirectSuperClasses">The names of the direct superclasses.</param>
    /// <param name="DirectSubclasses">The names of the direct subclasses.</param>
    /// <param name="AllAncestors">The names of every ancestor, nearest first.</param>
    /// <param name="AllDescendants">The names of every descendant.</param>
    /// <param name="OwnedAttributes">The owned features.</param>
    /// <param name="InheritedAttributes">The inherited features.</param>
    public sealed record ClassNode(
        string Name,
        string QualifiedName,
        string Package,
        bool IsAbstract,
        string? XmlElement,
        string? Documentation,
        IReadOnlyList<string> DirectSuperClasses,
        IReadOnlyList<string> DirectSubclasses,
        IReadOnlyList<string> AllAncestors,
        IReadOnlyList<string> AllDescendants,
        IReadOnlyList<FeatureNode> OwnedAttributes,
        IReadOnlyList<FeatureNode> InheritedAttributes);

    /// <summary>
    /// One feature in <c>metamodel.json</c>.
    /// </summary>
    /// <param name="Name">The feature name.</param>
    /// <param name="Owner">The name of the owning class.</param>
    /// <param name="Type">The type name.</param>
    /// <param name="TypeKind"><c>class</c> or <c>primitive</c>.</param>
    /// <param name="Lower">The lower bound.</param>
    /// <param name="Upper">The upper bound (<c>*</c> for unbounded).</param>
    /// <param name="IsComposite">Whether the feature is a containment.</param>
    /// <param name="IsOrdered">Whether the feature is ordered.</param>
    /// <param name="Kind"><c>value</c>, <c>composite</c> or <c>reference</c>.</param>
    /// <param name="Opposite">The opposite end's name, if any.</param>
    /// <param name="Documentation">The metamodel documentation.</param>
    /// <param name="Xml">How the feature is written in a file, or null when it is not serialized.</param>
    public sealed record FeatureNode(
        string Name,
        string Owner,
        string? Type,
        string TypeKind,
        int Lower,
        string Upper,
        bool IsComposite,
        bool IsOrdered,
        string Kind,
        string? Opposite,
        string? Documentation,
        XmlBindingNode? Xml);

    /// <summary>
    /// The XML binding of a feature in <c>metamodel.json</c>.
    /// </summary>
    /// <param name="Kind"><c>attribute</c> or <c>element</c>.</param>
    /// <param name="Name">The XML attribute or element name.</param>
    /// <param name="Type">The declared XML type, if typed directly.</param>
    /// <param name="Use">For an attribute, its use.</param>
    /// <param name="Occurs">For an element, its occurrence range.</param>
    /// <param name="InnerOccurs">For a wrapper element, the occurrence range of its contents.</param>
    /// <param name="Contains">For a wrapper element, the element names it may contain.</param>
    public sealed record XmlBindingNode(string Kind, string Name, string? Type, string? Use, string? Occurs, string? InnerOccurs, IReadOnlyList<string> Contains);

    /// <summary>
    /// One primitive type in <c>metamodel.json</c>.
    /// </summary>
    /// <param name="Name">The type name.</param>
    /// <param name="QualifiedName">The qualified name.</param>
    /// <param name="Package">The owning package.</param>
    public sealed record PrimitiveTypeNode(string Name, string QualifiedName, string Package);

    /// <summary>
    /// One row of <c>metamodel/index.json</c>.
    /// </summary>
    /// <param name="Name">The element name.</param>
    /// <param name="Kind"><c>class</c> or <c>primitiveType</c>.</param>
    /// <param name="Package">The owning package.</param>
    /// <param name="QualifiedName">The qualified name.</param>
    /// <param name="IsAbstract">Whether the element is abstract.</param>
    /// <param name="XmlElement">The XML element name, if the element is written as one.</param>
    /// <param name="File">The page path relative to <c>metamodel/</c>.</param>
    public sealed record MetamodelIndexRow(string Name, string Kind, string Package, string QualifiedName, bool IsAbstract, string? XmlElement, string File);

    /// <summary>
    /// One row of <c>schema/index.json</c>.
    /// </summary>
    /// <param name="Name">The schema type name.</param>
    /// <param name="Kind"><c>complexType</c> or <c>simpleType</c>.</param>
    /// <param name="MetamodelClass">The metamodel class it serializes, if any.</param>
    /// <param name="GlobalElement">The global element declared with this type, if any.</param>
    /// <param name="File">The page path relative to <c>schema/</c>.</param>
    public sealed record SchemaIndexRow(string Name, string Kind, string? MetamodelClass, string? GlobalElement, string File);
}
