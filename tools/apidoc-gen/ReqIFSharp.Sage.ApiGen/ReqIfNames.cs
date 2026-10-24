// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIfNames.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Naming conventions of the ReqIF XML schema: element names are the upper-kebab-case form of the
    /// metaclass name (<c>SpecObject</c> becomes <c>SPEC-OBJECT</c>, <c>ReqIFHeader</c> becomes
    /// <c>REQ-IF-HEADER</c>), and metaclass features are lower camel case (<c>longName</c>).
    /// </summary>
    public static class ReqIfNames
    {
        /// <summary>
        /// ReqIFSharp class names whose spelling differs from the ReqIF 1.2 metaclass name.
        /// </summary>
        private static readonly Dictionary<string, string> MetaclassNameOverrides = new(StringComparer.Ordinal)
        {
            ["AlternativeId"] = "AlternativeID",
        };

        /// <summary>
        /// ReqIFSharp property names (keyed <c>Class.Property</c>) whose lower camel case form differs from the
        /// ReqIF 1.2 metamodel feature name; each listed feature's upper-kebab-case form is the XSD name.
        /// </summary>
        private static readonly Dictionary<string, string> FeatureNameOverrides = new(StringComparer.Ordinal)
        {
            ["AttributeDefinitionEnumeration.IsMultiValued"] = "multiValued",
            ["Identifiable.AlternativeId"] = "alternativeID",
            ["Identifiable.Description"] = "desc",
            ["ReqIF.ToolExtension"] = "toolExtensions",
            ["ReqIFContent.DataTypes"] = "datatypes",
        };

        /// <summary>
        /// Gets the ReqIF 1.2 metaclass name of a ReqIFSharp class, e.g. <c>AlternativeId</c> is the
        /// <c>AlternativeID</c> metaclass; most names are identical.
        /// </summary>
        /// <param name="className">The C# class name.</param>
        /// <returns>The metaclass name.</returns>
        public static string MetaclassName(string className)
        {
            return MetaclassNameOverrides.TryGetValue(className, out var name) ? name : className;
        }

        /// <summary>
        /// Gets the ReqIF 1.2 feature name of a ReqIFSharp property: the lower camel case property name
        /// (<see cref="ToFeatureName"/>) unless the library spells it differently (<c>Description</c> is the
        /// <c>desc</c> feature, <c>DataTypes</c> is <c>datatypes</c>).
        /// </summary>
        /// <param name="className">The C# name of the class declaring the property.</param>
        /// <param name="propertyName">The C# property name.</param>
        /// <returns>The feature name.</returns>
        public static string FeatureName(string className, string propertyName)
        {
            return FeatureNameOverrides.TryGetValue(className + "." + propertyName, out var name) ? name : ToFeatureName(propertyName);
        }

        /// <summary>
        /// Converts a PascalCase metaclass (or feature) name to its ReqIF XML element name. A word starts
        /// at every lower-to-upper case transition, and an acronym run ends before its last capital when
        /// that capital starts a new lower case word (<c>XHTMLContent</c> is <c>XHTML-CONTENT</c>).
        /// </summary>
        /// <param name="name">The PascalCase name, e.g. <c>DatatypeDefinitionXHTML</c>.</param>
        /// <returns>The upper-kebab-case name, e.g. <c>DATATYPE-DEFINITION-XHTML</c>.</returns>
        public static string ToXmlElementName(string name)
        {
            var words = SplitWords(name);
            var builder = new StringBuilder();

            foreach (var word in words)
            {
                if (builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(word.ToUpperInvariant());
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts a PascalCase C# member name to lower camel case, the spelling the ReqIF metamodel uses
        /// for features: <c>LongName</c> becomes <c>longName</c>, <c>IsTableInternal</c> becomes
        /// <c>isTableInternal</c>, and a leading acronym is lowered as a whole (<c>ID</c> becomes <c>id</c>).
        /// </summary>
        /// <param name="name">The PascalCase name.</param>
        /// <returns>The lower camel case name.</returns>
        public static string ToFeatureName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var words = SplitWords(name);
            var builder = new StringBuilder();
            builder.Append(words[0].ToLowerInvariant());

            for (var index = 1; index < words.Count; index++)
            {
                builder.Append(words[index]);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Splits a PascalCase identifier into its words.
        /// </summary>
        /// <param name="name">The identifier.</param>
        /// <returns>The words, in order, with their original casing.</returns>
        public static IReadOnlyList<string> SplitWords(string name)
        {
            var words = new List<string>();
            var current = new StringBuilder();

            for (var index = 0; index < name.Length; index++)
            {
                var character = name[index];

                if (character is '_' or '-' or ' ')
                {
                    Flush(words, current);
                    continue;
                }

                if (current.Length > 0)
                {
                    var previous = name[index - 1];
                    var next = index + 1 < name.Length ? name[index + 1] : '\0';
                    var category = CharUnicodeInfo.GetUnicodeCategory(character);

                    var lowerToUpper = char.IsUpper(character) && (char.IsLower(previous) || char.IsDigit(previous));
                    var acronymEnd = char.IsUpper(character) && char.IsUpper(previous) && char.IsLower(next);

                    if ((lowerToUpper || acronymEnd) && category == UnicodeCategory.UppercaseLetter)
                    {
                        Flush(words, current);
                    }
                }

                current.Append(character);
            }

            Flush(words, current);
            return words;
        }

        private static void Flush(List<string> words, StringBuilder current)
        {
            if (current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }
        }
    }
}
