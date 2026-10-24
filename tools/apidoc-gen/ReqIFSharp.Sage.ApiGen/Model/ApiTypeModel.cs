// -------------------------------------------------------------------------------------------------
// <copyright file="ApiTypeModel.cs" company="Starion Group S.A.">
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

    using ReqIFSharp.Sage.ApiGen.Documentation;

    /// <summary>
    /// A reflected public type of a documented assembly, with its public members, documentation and ReqIF
    /// mapping. Built by <see cref="ApiModelBuilder"/>.
    /// </summary>
    public sealed class ApiTypeModel
    {
        /// <summary>
        /// Gets the reflected type.
        /// </summary>
        public required Type Type { get; init; }

        /// <summary>
        /// Gets the C# name, e.g. <c>SpecObject</c> or <c>Outer.Inner</c> for a nested type.
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// Gets the full name in documentation-id spelling (nested types joined with <c>.</c>), e.g.
        /// <c>ReqIFSharp.SpecObject</c>; also the page's file name stem.
        /// </summary>
        public required string FullName { get; init; }

        /// <summary>
        /// Gets the namespace.
        /// </summary>
        public required string Namespace { get; init; }

        /// <summary>
        /// Gets the package the type ships in.
        /// </summary>
        public required ApiPackageModel Package { get; init; }

        /// <summary>
        /// Gets the kind: <c>class</c>, <c>interface</c>, <c>enum</c>, <c>struct</c> or <c>delegate</c>.
        /// </summary>
        public required string Kind { get; init; }

        /// <summary>
        /// Gets a value indicating whether the type is an abstract (non-static) class.
        /// </summary>
        public required bool IsAbstract { get; init; }

        /// <summary>
        /// Gets a value indicating whether the type is a static class.
        /// </summary>
        public required bool IsStatic { get; init; }

        /// <summary>
        /// Gets a value indicating whether the type is a sealed (non-static) class.
        /// </summary>
        public required bool IsSealed { get; init; }

        /// <summary>
        /// Gets the C# declaration line, e.g. <c>public class SpecObject : SpecElementWithAttributes</c>.
        /// </summary>
        public required string Declaration { get; init; }

        /// <summary>
        /// Gets the direct base type (other than <see cref="object"/> and the implicit enum/struct/delegate bases), or <see langword="null"/>.
        /// </summary>
        public required Type? BaseType { get; init; }

        /// <summary>
        /// Gets the base type chain from the root down to the direct base type (excluding the type itself).
        /// </summary>
        public required IReadOnlyList<Type> InheritanceChain { get; init; }

        /// <summary>
        /// Gets all implemented interfaces, ordered ordinally by full name.
        /// </summary>
        public required IReadOnlyList<Type> Interfaces { get; init; }

        /// <summary>
        /// Gets the ReqIF metaclass name the type implements (e.g. <c>SpecObject</c>), or <see langword="null"/>.
        /// </summary>
        public required string? ReqIfClass { get; init; }

        /// <summary>
        /// Gets the ReqIF XML element name (e.g. <c>SPEC-OBJECT</c>), or <see langword="null"/> when the type is
        /// not a ReqIF metaclass or is abstract.
        /// </summary>
        public required string? XmlElement { get; init; }

        /// <summary>
        /// Gets a note about the ReqIF mapping, e.g. that a class carries the <c>ReqIfClass</c> attribute but is
        /// not a ReqIF metaclass, or <see langword="null"/>.
        /// </summary>
        public string? ReqIfNote { get; init; }

        /// <summary>
        /// Gets the documentation id, e.g. <c>T:ReqIFSharp.SpecObject</c>.
        /// </summary>
        public required string DocId { get; init; }

        /// <summary>
        /// Gets the resolved documentation.
        /// </summary>
        public required DocComment Doc { get; init; }

        /// <summary>
        /// Gets the public constructors.
        /// </summary>
        public required IReadOnlyList<ApiMethodModel> Constructors { get; init; }

        /// <summary>
        /// Gets the public properties declared by this type.
        /// </summary>
        public required IReadOnlyList<ApiPropertyModel> Properties { get; init; }

        /// <summary>
        /// Gets the public methods declared by this type (including operators and extension methods).
        /// </summary>
        public required IReadOnlyList<ApiMethodModel> Methods { get; init; }

        /// <summary>
        /// Gets the public events declared by this type.
        /// </summary>
        public required IReadOnlyList<ApiEventModel> Events { get; init; }

        /// <summary>
        /// Gets the public fields declared by this type (for enumerations: the literals, in value order).
        /// </summary>
        public required IReadOnlyList<ApiFieldModel> Fields { get; init; }

        /// <summary>
        /// Gets the documented types that derive directly from (or, for interfaces, directly implement) this type.
        /// </summary>
        public List<ApiTypeModel> DerivedTypes { get; } = new();

        /// <summary>
        /// Gets the extension methods of the documented assemblies whose <c>this</c> parameter accepts this type.
        /// </summary>
        public List<ApiMethodModel> AvailableExtensionMethods { get; } = new();
    }
}
