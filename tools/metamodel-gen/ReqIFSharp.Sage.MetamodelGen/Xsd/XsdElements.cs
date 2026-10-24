// -------------------------------------------------------------------------------------------------
// <copyright file="XsdElements.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen.Xsd
{
    using System.Collections.Generic;

    /// <summary>
    /// The parts of an XML schema that the ReqIF knowledge base describes.
    /// </summary>
    /// <param name="TargetNamespace">The schema's target namespace.</param>
    /// <param name="GlobalElements">The top-level <c>xsd:element</c> declarations.</param>
    /// <param name="ComplexTypes">The named complex types, sorted ordinally by name.</param>
    /// <param name="SimpleTypes">The named simple types, sorted ordinally by name.</param>
    /// <param name="Imports">The <c>xsd:import</c>/<c>xsd:include</c> declarations.</param>
    /// <param name="Groups">The named model groups (used by <c>driver.xsd</c> to define the XHTML subset).</param>
    public sealed record XsdSchema(
        string? TargetNamespace,
        IReadOnlyList<XsdGlobalElement> GlobalElements,
        IReadOnlyList<XsdComplexType> ComplexTypes,
        IReadOnlyList<XsdSimpleType> SimpleTypes,
        IReadOnlyList<XsdImport> Imports,
        IReadOnlyList<XsdGroup> Groups);

    /// <summary>
    /// A top-level element declaration, such as <c>&lt;xsd:element name="REQ-IF" type="REQIF:REQ-IF"/&gt;</c>.
    /// </summary>
    /// <param name="Name">The element name.</param>
    /// <param name="Type">The declared type, without namespace prefix.</param>
    public sealed record XsdGlobalElement(string Name, string? Type);

    /// <summary>
    /// A named complex type.
    /// </summary>
    /// <param name="Name">The type name, e.g. <c>SPEC-OBJECT</c>.</param>
    /// <param name="Compositor">The top-level compositor: <c>all</c>, <c>sequence</c>, <c>choice</c>, <c>group</c> or <c>empty</c>.</param>
    /// <param name="Attributes">The XML attributes, in document order.</param>
    /// <param name="Children">The child elements, in document order.</param>
    /// <param name="GroupReference">The referenced model group when the content is an <c>xsd:group ref</c>.</param>
    /// <param name="Wildcard">The <c>xsd:any</c> wildcard directly in the content model, if any.</param>
    public sealed record XsdComplexType(
        string Name,
        string Compositor,
        IReadOnlyList<XsdAttribute> Attributes,
        IReadOnlyList<XsdChildElement> Children,
        string? GroupReference,
        XsdWildcard? Wildcard);

    /// <summary>
    /// An XML attribute declaration.
    /// </summary>
    /// <param name="Name">The attribute name (or the referenced attribute, e.g. <c>xml:lang</c>).</param>
    /// <param name="Type">The declared type, without namespace prefix.</param>
    /// <param name="Use"><c>required</c> or <c>optional</c>.</param>
    /// <param name="Default">The default value, if any.</param>
    /// <param name="Fixed">The fixed value, if any.</param>
    public sealed record XsdAttribute(string Name, string? Type, string Use, string? Default, string? Fixed);

    /// <summary>
    /// A child element of a complex type. In ReqIF most children are "wrapper" elements whose anonymous
    /// type is a choice of typed elements or <c>-REF</c> references; <see cref="Options"/> lists those.
    /// </summary>
    /// <param name="Name">The element name, e.g. <c>VALUES</c>.</param>
    /// <param name="MinOccurs">The minOccurs value (default <c>1</c>).</param>
    /// <param name="MaxOccurs">The maxOccurs value (default <c>1</c>, <c>unbounded</c> when unbounded).</param>
    /// <param name="Type">The declared type when the element is typed directly (e.g. <c>XHTML-CONTENT</c>).</param>
    /// <param name="Options">The elements permitted inside an anonymous wrapper type.</param>
    /// <param name="OptionsMinOccurs">The minOccurs of the wrapper's inner compositor.</param>
    /// <param name="OptionsMaxOccurs">The maxOccurs of the wrapper's inner compositor.</param>
    /// <param name="Wildcard">An <c>xsd:any</c> inside the wrapper, if any.</param>
    public sealed record XsdChildElement(
        string Name,
        string MinOccurs,
        string MaxOccurs,
        string? Type,
        IReadOnlyList<XsdChoiceOption> Options,
        string? OptionsMinOccurs,
        string? OptionsMaxOccurs,
        XsdWildcard? Wildcard);

    /// <summary>
    /// One element permitted inside a wrapper element.
    /// </summary>
    /// <param name="Name">The element name, e.g. <c>SPEC-OBJECT-TYPE-REF</c>.</param>
    /// <param name="Type">The declared type, e.g. <c>LOCAL-REF</c> or <c>SPEC-OBJECT</c>.</param>
    public sealed record XsdChoiceOption(string Name, string? Type)
    {
        /// <summary>
        /// Gets a value indicating whether this option is a reference to another element by identifier
        /// rather than a contained element.
        /// </summary>
        public bool IsReference => this.Type is "LOCAL-REF" or "GLOBAL-REF";
    }

    /// <summary>
    /// An <c>xsd:any</c> wildcard.
    /// </summary>
    /// <param name="Namespace">The namespace constraint (e.g. <c>##other</c>).</param>
    /// <param name="ProcessContents">The processContents value (<c>strict</c>, <c>lax</c> or <c>skip</c>).</param>
    /// <param name="MinOccurs">The minOccurs value.</param>
    /// <param name="MaxOccurs">The maxOccurs value.</param>
    public sealed record XsdWildcard(string Namespace, string ProcessContents, string MinOccurs, string MaxOccurs);

    /// <summary>
    /// A named simple type.
    /// </summary>
    /// <param name="Name">The type name, e.g. <c>LOCAL-REF</c>.</param>
    /// <param name="Base">The restriction base type, e.g. <c>xsd:IDREF</c>.</param>
    public sealed record XsdSimpleType(string Name, string? Base);

    /// <summary>
    /// An <c>xsd:import</c> or <c>xsd:include</c>.
    /// </summary>
    /// <param name="Kind"><c>import</c> or <c>include</c>.</param>
    /// <param name="Namespace">The imported namespace, if any.</param>
    /// <param name="SchemaLocation">The schema location.</param>
    public sealed record XsdImport(string Kind, string? Namespace, string? SchemaLocation);

    /// <summary>
    /// A named model group: the element names it permits directly and the groups it pulls in.
    /// </summary>
    /// <param name="Name">The group name, e.g. <c>xhtml.BlkStruct.class</c>.</param>
    /// <param name="Elements">The element names declared directly in the group, in document order.</param>
    /// <param name="GroupReferences">The names of groups referenced from this group, in document order.</param>
    public sealed record XsdGroup(string Name, IReadOnlyList<string> Elements, IReadOnlyList<string> GroupReferences);
}
