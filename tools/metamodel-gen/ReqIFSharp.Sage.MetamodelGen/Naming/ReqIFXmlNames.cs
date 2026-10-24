// -------------------------------------------------------------------------------------------------
// <copyright file="ReqIFXmlNames.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.MetamodelGen.Naming
{
    using System.Text;

    /// <summary>
    /// The ReqIF XML schema's naming rule: metamodel class and feature names are written in upper
    /// case with a hyphen at every word boundary (<c>SpecObject</c> becomes <c>SPEC-OBJECT</c>,
    /// <c>longName</c> becomes <c>LONG-NAME</c>, <c>ReqIFHeader</c> becomes <c>REQ-IF-HEADER</c>).
    /// </summary>
    public static class ReqIFXmlNames
    {
        /// <summary>
        /// Converts a metamodel class or feature name to its ReqIF XML name.
        /// </summary>
        /// <param name="name">A PascalCase or camelCase name such as <c>DatatypeDefinitionXHTML</c>.</param>
        /// <returns>The upper-case, hyphenated XML name such as <c>DATATYPE-DEFINITION-XHTML</c>.</returns>
        /// <remarks>
        /// A boundary falls between a lower-case letter or digit and an upper-case letter, and before
        /// the last capital of a run of capitals that is followed by a lower-case letter (so the
        /// acronym in <c>ReqIFHeader</c> stays one word). Trailing underscores (an Enterprise
        /// Architect artifact such as <c>attributeValue_</c>) are dropped.
        /// </remarks>
        public static string ToXmlName(string name)
        {
            var trimmed = name.TrimEnd('_');
            var builder = new StringBuilder(trimmed.Length + 8);

            for (var index = 0; index < trimmed.Length; index++)
            {
                var current = trimmed[index];
                if (index > 0 && char.IsUpper(current))
                {
                    var previous = trimmed[index - 1];
                    var nextIsLower = index + 1 < trimmed.Length && char.IsLower(trimmed[index + 1]);
                    if (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower))
                    {
                        builder.Append('-');
                    }
                }

                builder.Append(char.ToUpperInvariant(current));
            }

            return builder.ToString();
        }
    }
}
