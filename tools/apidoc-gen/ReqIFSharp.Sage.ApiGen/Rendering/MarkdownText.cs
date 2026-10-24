// -------------------------------------------------------------------------------------------------
// <copyright file="MarkdownText.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Rendering
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Escaping helpers for the generated markdown and its YAML front matter.
    /// </summary>
    public static class MarkdownText
    {
        /// <summary>
        /// Escapes a value for a markdown table cell: <c>|</c> becomes <c>\|</c> and line breaks become spaces.
        /// </summary>
        /// <param name="text">The cell text.</param>
        /// <returns>The escaped text.</returns>
        public static string EscapeTableCell(string text)
        {
            return text.Replace("\r\n", " ").Replace('\n', ' ').Replace("|", "\\|", StringComparison.Ordinal);
        }

        /// <summary>
        /// Wraps <paramref name="code"/> in a markdown code span, so generic angle brackets survive rendering.
        /// Uses a longer backtick fence when the code itself contains backticks.
        /// </summary>
        /// <param name="code">The code text.</param>
        /// <returns>The code span.</returns>
        public static string Code(string code)
        {
            if (!code.Contains('`', StringComparison.Ordinal))
            {
                return "`" + code + "`";
            }

            return "`` " + code + " ``";
        }

        /// <summary>
        /// Renders a code span for use inside a table cell (the code span plus <c>|</c> escaping).
        /// </summary>
        /// <param name="code">The code text.</param>
        /// <returns>The escaped code span.</returns>
        public static string CodeCell(string code)
        {
            return EscapeTableCell(Code(code));
        }

        /// <summary>
        /// Renders a YAML double-quoted scalar.
        /// </summary>
        /// <param name="value">The value, or <see langword="null"/> for YAML <c>null</c>.</param>
        /// <returns>The YAML scalar.</returns>
        public static string YamlString(string? value)
        {
            if (value is null)
            {
                return "null";
            }

            var builder = new StringBuilder("\"");
            foreach (var character in value)
            {
                switch (character)
                {
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\r':
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }

            return builder.Append('"').ToString();
        }

        /// <summary>
        /// Renders a YAML flow sequence of double-quoted strings, e.g. <c>["A", "B"]</c>.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <returns>The YAML sequence.</returns>
        public static string YamlList(IEnumerable<string> values)
        {
            return "[" + string.Join(", ", values.Select(YamlString)) + "]";
        }

        /// <summary>
        /// Renders a YAML boolean.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> or <c>false</c>.</returns>
        public static string YamlBool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
