// -------------------------------------------------------------------------------------------------
// <copyright file="MetamodelView.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.MetamodelGen
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using ReqIFSharp.Sage.MetamodelGen.Cmof;
    using ReqIFSharp.Sage.MetamodelGen.Naming;
    using ReqIFSharp.Sage.MetamodelGen.Xsd;

    /// <summary>
    /// The CMOF metamodel joined with the XML schema: inheritance closures for every class and, for every
    /// feature, how it is written in a <c>.reqif</c> file.
    /// </summary>
    public sealed class MetamodelView
    {
        private readonly Dictionary<string, XsdComplexType> complexTypesByName;

        private readonly Dictionary<string, IReadOnlyList<CmofClass>> directSubclasses;

        /// <summary>
        /// Initializes a new instance of the <see cref="MetamodelView"/> class.
        /// </summary>
        /// <param name="model">The CMOF metamodel.</param>
        /// <param name="schema">The ReqIF XML schema, or null when it is unavailable.</param>
        public MetamodelView(CmofModel model, XsdSchema? schema)
        {
            this.Model = model;
            this.Schema = schema;
            this.complexTypesByName = schema?.ComplexTypes.ToDictionary(t => t.Name, StringComparer.Ordinal)
                ?? new Dictionary<string, XsdComplexType>(StringComparer.Ordinal);

            this.directSubclasses = model.Classes.ToDictionary(
                c => c.Id,
                c => (IReadOnlyList<CmofClass>)model.Classes.Where(s => s.SuperClassIds.Contains(c.Id)).OrderBy(s => s.Name, StringComparer.Ordinal).ToList(),
                StringComparer.Ordinal);
        }

        /// <summary>
        /// Gets the CMOF metamodel.
        /// </summary>
        public CmofModel Model { get; }

        /// <summary>
        /// Gets the XML schema, or null when it is unavailable.
        /// </summary>
        public XsdSchema? Schema { get; }

        /// <summary>
        /// Gets the direct superclasses of <paramref name="class"/>, sorted by name.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>The direct superclasses.</returns>
        public IReadOnlyList<CmofClass> DirectSuperClassesOf(CmofClass @class)
        {
            return @class.SuperClassIds.Select(this.Model.FindClass).OfType<CmofClass>().OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Gets the direct subclasses of <paramref name="class"/>, sorted by name.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>The direct subclasses.</returns>
        public IReadOnlyList<CmofClass> DirectSubclassesOf(CmofClass @class)
        {
            return this.directSubclasses.TryGetValue(@class.Id, out var found) ? found : [];
        }

        /// <summary>
        /// Gets every ancestor of <paramref name="class"/>, nearest first (breadth-first), without duplicates.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>The ancestors.</returns>
        public IReadOnlyList<CmofClass> AllAncestorsOf(CmofClass @class)
        {
            var result = new List<CmofClass>();
            var seen = new HashSet<string>(StringComparer.Ordinal) { @class.Id };
            var queue = new Queue<CmofClass>(this.DirectSuperClassesOf(@class));

            while (queue.Count > 0)
            {
                var next = queue.Dequeue();
                if (!seen.Add(next.Id))
                {
                    continue;
                }

                result.Add(next);
                foreach (var parent in this.DirectSuperClassesOf(next))
                {
                    queue.Enqueue(parent);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets every descendant of <paramref name="class"/>, sorted by name.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>The descendants.</returns>
        public IReadOnlyList<CmofClass> AllDescendantsOf(CmofClass @class)
        {
            var result = new Dictionary<string, CmofClass>(StringComparer.Ordinal);
            var stack = new Stack<CmofClass>(this.DirectSubclassesOf(@class));

            while (stack.Count > 0)
            {
                var next = stack.Pop();
                if (next.Id == @class.Id || !result.TryAdd(next.Id, next))
                {
                    continue;
                }

                foreach (var child in this.DirectSubclassesOf(next))
                {
                    stack.Push(child);
                }
            }

            return result.Values.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// Gets the features <paramref name="class"/> inherits, each with the ancestor that owns it, ordered
        /// nearest ancestor first and then by feature name.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>The inherited features.</returns>
        public IReadOnlyList<(CmofClass Owner, CmofProperty Feature)> InheritedFeaturesOf(CmofClass @class)
        {
            return this.AllAncestorsOf(@class)
                .SelectMany(a => a.OwnedAttributes.OrderBy(p => p.Name, StringComparer.Ordinal).Select(p => (Owner: a, Feature: p)))
                .ToList();
        }

        /// <summary>
        /// Gets the XML element name the concrete <paramref name="class"/> is written as, or null for an
        /// abstract class or one the schema has no complex type for.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>The element name, such as <c>SPEC-OBJECT</c>.</returns>
        public string? XmlElementOf(CmofClass @class)
        {
            return this.ComplexTypeOf(@class)?.Name;
        }

        /// <summary>
        /// Gets the schema complex type the concrete <paramref name="class"/> is written with.
        /// </summary>
        /// <param name="class">The class.</param>
        /// <returns>The complex type, or null for an abstract class or when the schema has none.</returns>
        public XsdComplexType? ComplexTypeOf(CmofClass @class)
        {
            if (@class.IsAbstract)
            {
                return null;
            }

            return this.complexTypesByName.TryGetValue(ReqIFXmlNames.ToXmlName(@class.Name), out var found) ? found : null;
        }

        /// <summary>
        /// Finds the CMOF class that a schema complex type serializes.
        /// </summary>
        /// <param name="complexType">The complex type.</param>
        /// <returns>The class, or null when none matches (e.g. <c>XHTML-CONTENT</c>).</returns>
        public CmofClass? ClassOf(XsdComplexType complexType)
        {
            return this.Model.Classes.FirstOrDefault(c => !c.IsAbstract && ReqIFXmlNames.ToXmlName(c.Name) == complexType.Name);
        }

        /// <summary>
        /// Works out how <paramref name="feature"/> is written in a file when it appears on the concrete
        /// class <paramref name="class"/> (or, for an abstract class, on its first concrete descendant that
        /// the schema knows).
        /// </summary>
        /// <param name="class">The class the feature is viewed from.</param>
        /// <param name="feature">The feature (owned or inherited).</param>
        /// <returns>The binding; <see cref="FeatureXmlBinding.Kind"/> is <c>none</c> when the feature is not serialized.</returns>
        public FeatureXmlBinding BindingOf(CmofClass @class, CmofProperty feature)
        {
            var complexType = this.ComplexTypeOf(@class)
                ?? this.AllDescendantsOf(@class).Select(this.ComplexTypeOf).FirstOrDefault(t => t is not null && Bind(t, feature).Kind != "none");

            return complexType is null ? FeatureXmlBinding.None : Bind(complexType, feature);
        }

        /// <summary>
        /// Gets the XML attributes and child elements of <paramref name="complexType"/> that no feature of
        /// its class is bound to (such as <c>xml:lang</c> on <c>REQ-IF</c>).
        /// </summary>
        /// <param name="class">The class the complex type serializes.</param>
        /// <param name="complexType">The complex type.</param>
        /// <returns>The unbound attribute and element names.</returns>
        public IReadOnlyList<string> UnboundSchemaItemsOf(CmofClass @class, XsdComplexType complexType)
        {
            var features = @class.OwnedAttributes.Concat(this.InheritedFeaturesOf(@class).Select(f => f.Feature)).ToList();
            var bound = features.Select(f => Bind(complexType, f)).Where(b => b.Kind != "none").Select(b => b.XmlName).ToHashSet(StringComparer.Ordinal);

            return complexType.Attributes.Select(a => a.Name)
                .Concat(complexType.Children.Select(c => c.Name))
                .Where(n => !bound.Contains(n))
                .ToList();
        }

        private static FeatureXmlBinding Bind(XsdComplexType complexType, CmofProperty feature)
        {
            var xmlName = ReqIFXmlNames.ToXmlName(feature.Name);

            var attribute = complexType.Attributes.FirstOrDefault(a => a.Name == xmlName);
            if (attribute is not null)
            {
                return new FeatureXmlBinding("attribute", xmlName, attribute.Type, attribute.Use, null, null, []);
            }

            var child = complexType.Children.FirstOrDefault(c => c.Name == xmlName);
            if (child is not null)
            {
                var occurs = $"{child.MinOccurs}..{child.MaxOccurs}";
                var innerOccurs = child.OptionsMinOccurs is null ? null : $"{child.OptionsMinOccurs}..{child.OptionsMaxOccurs}";
                return new FeatureXmlBinding("element", xmlName, child.Type, null, occurs, innerOccurs, child.Options);
            }

            return FeatureXmlBinding.None;
        }
    }

    /// <summary>
    /// How one metamodel feature is written in a ReqIF XML document.
    /// </summary>
    /// <param name="Kind"><c>attribute</c> (an XML attribute), <c>element</c> (a child element) or <c>none</c> (not serialized).</param>
    /// <param name="XmlName">The XML attribute or element name.</param>
    /// <param name="Type">The declared XML type, when the attribute or element is typed directly.</param>
    /// <param name="Use">For an XML attribute, <c>required</c> or <c>optional</c>.</param>
    /// <param name="Occurs">For a child element, its occurrence range (e.g. <c>0..1</c>).</param>
    /// <param name="InnerOccurs">For a wrapper element, the occurrence range of its contents (e.g. <c>0..unbounded</c>).</param>
    /// <param name="Options">For a wrapper element, the elements it may contain.</param>
    public sealed record FeatureXmlBinding(
        string Kind,
        string XmlName,
        string? Type,
        string? Use,
        string? Occurs,
        string? InnerOccurs,
        IReadOnlyList<XsdChoiceOption> Options)
    {
        /// <summary>
        /// The binding for a feature that is not written to the file (a non-navigable or derived end).
        /// </summary>
        public static readonly FeatureXmlBinding None = new("none", string.Empty, null, null, null, null, []);

        /// <summary>
        /// Gets a short, human-readable description, e.g. <c>attribute LONG-NAME (optional)</c> or
        /// <c>&lt;TYPE&gt; → &lt;SPEC-OBJECT-TYPE-REF&gt;</c>.
        /// </summary>
        public string Describe()
        {
            return this.Kind switch
            {
                "attribute" => $"attribute `{this.XmlName}` ({this.Use})",
                "element" when this.Options.Count > 0 => $"`<{this.XmlName}>` → " + string.Join(" \\| ", this.Options.Select(o => $"`<{o.Name}>`")),
                "element" => $"`<{this.XmlName}>`" + (this.Type is null ? string.Empty : $" : {this.Type}"),
                _ => "not serialized",
            };
        }
    }
}
