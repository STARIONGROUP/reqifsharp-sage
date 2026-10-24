// -------------------------------------------------------------------------------------------------
// <copyright file="DocMarkdownRenderer.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.ApiGen.Documentation
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    using ReqIFSharp.Sage.ApiGen.Rendering;

    /// <summary>
    /// Renders XML documentation content (<c>&lt;summary&gt;</c>, <c>&lt;remarks&gt;</c>, ...) to markdown:
    /// <c>&lt;see cref/&gt;</c> becomes a code span (a link when the link resolver knows the target),
    /// <c>&lt;see langword/&gt;</c>, <c>&lt;paramref/&gt;</c>, <c>&lt;typeparamref/&gt;</c> and
    /// <c>&lt;c&gt;</c> become code spans, <c>&lt;code&gt;</c> a fenced block, <c>&lt;para&gt;</c> a paragraph,
    /// <c>&lt;list&gt;</c> a bullet list. Whitespace inside text runs is collapsed.
    /// </summary>
    public sealed class DocMarkdownRenderer
    {
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.CultureInvariant);

        private static readonly Regex FencedBlock = new(@"```csharp\n(.*?)\n```", RegexOptions.CultureInvariant | RegexOptions.Singleline);

        private static readonly Regex BlankLines =new(@"\n{3,}", RegexOptions.CultureInvariant);

        private readonly Func<string, string?>? linkResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocMarkdownRenderer"/> class.
        /// </summary>
        /// <param name="linkResolver">Maps a <c>cref</c> documentation id to a relative link target, or returns
        /// <see langword="null"/> when the target has no page; <see langword="null"/> renders no links at all.</param>
        public DocMarkdownRenderer(Func<string, string?>? linkResolver = null)
        {
            this.linkResolver = linkResolver;
        }

        /// <summary>
        /// Renders <paramref name="element"/>'s content as (possibly multi-paragraph) markdown.
        /// </summary>
        /// <param name="element">The documentation element, or <see langword="null"/>.</param>
        /// <returns>The markdown, or <see langword="null"/> when the element is missing or empty.</returns>
        public string? Render(XElement? element)
        {
            if (element is null)
            {
                return null;
            }

            var builder = new StringBuilder();
            this.AppendNodes(builder, element.Nodes());
            var text = Normalize(builder.ToString());
            return text.Length == 0 ? null : text;
        }

        /// <summary>
        /// Renders <paramref name="element"/>'s content on a single line, suitable for a markdown table cell:
        /// line breaks become spaces and <c>|</c> is escaped.
        /// </summary>
        /// <param name="element">The documentation element, or <see langword="null"/>.</param>
        /// <returns>The single-line markdown, or an empty string.</returns>
        public string RenderInline(XElement? element)
        {
            var text = this.Render(element);
            if (text is null)
            {
                return string.Empty;
            }

            var withoutBlocks = FencedBlock.Replace(text, match => MarkdownText.Code(Whitespace.Replace(match.Groups[1].Value, " ").Trim()));
            var singleLine = Whitespace.Replace(withoutBlocks, " ").Trim();
            return MarkdownText.EscapeTableCell(singleLine);
        }

        /// <summary>
        /// Gets the display text of a <c>cref</c> documentation id: the simple type name, or
        /// <c>Type.Member</c> for members, with generic arities shown as type parameter lists.
        /// </summary>
        /// <param name="cref">The id, e.g. <c>T:ReqIFSharp.SpecObject</c> or
        /// <c>M:ReqIFSharp.ReqIFDeserializer.Deserialize(System.String)</c>.</param>
        /// <returns>The display text, e.g. <c>SpecObject</c> or <c>ReqIFDeserializer.Deserialize</c>.</returns>
        public static string CrefDisplayName(string cref)
        {
            var body = cref.Length > 2 && cref[1] == ':' ? cref[2..] : cref;
            var kind = cref.Length > 2 && cref[1] == ':' ? cref[0] : 'T';

            var parenthesis = body.IndexOf('(', StringComparison.Ordinal);
            if (parenthesis >= 0)
            {
                body = body[..parenthesis];
            }

            var parts = SplitOutsideBraces(body);
            var keep = kind is 'T' or 'N' or '!' ? 1 : 2;
            var shown = parts.Skip(Math.Max(0, parts.Count - keep)).Select(ReadableArity);
            var display = string.Join(".", shown).Replace("#ctor", "ctor", StringComparison.Ordinal);
            return kind == 'N' ? body : display;
        }

        private void AppendNodes(StringBuilder builder, IEnumerable<XNode> nodes)
        {
            foreach (var node in nodes)
            {
                switch (node)
                {
                    case XText text:
                        builder.Append(Whitespace.Replace(text.Value, " "));
                        break;
                    case XElement element:
                        this.AppendElement(builder, element);
                        break;
                }
            }
        }

        private void AppendElement(StringBuilder builder, XElement element)
        {
            switch (element.Name.LocalName)
            {
                case "see":
                case "seealso":
                    this.AppendSee(builder, element);
                    break;
                case "paramref":
                case "typeparamref":
                    builder.Append('`').Append((string?)element.Attribute("name")).Append('`');
                    break;
                case "c":
                    builder.Append('`').Append(Whitespace.Replace(element.Value, " ").Trim()).Append('`');
                    break;
                case "code":
                    var code = Dedent(element.Value);
                    if (code.Contains('\n', StringComparison.Ordinal))
                    {
                        builder.Append("\n\n```csharp\n").Append(code).Append("\n```\n\n");
                    }
                    else
                    {
                        // A one-line <code> used inside a sentence reads better as a code span.
                        builder.Append(MarkdownText.Code(code.Trim()));
                    }

                    break;
                case "para":
                    builder.Append("\n\n");
                    this.AppendNodes(builder, element.Nodes());
                    builder.Append("\n\n");
                    break;
                case "br":
                    builder.Append('\n');
                    break;
                case "list":
                    this.AppendList(builder, element);
                    break;
                case "a":
                    var href = (string?)element.Attribute("href");
                    var linkText = Whitespace.Replace(element.Value, " ").Trim();
                    builder.Append('[').Append(linkText.Length == 0 ? href : linkText).Append("](").Append(href).Append(')');
                    break;
                case "b":
                case "strong":
                    builder.Append("**");
                    this.AppendNodes(builder, element.Nodes());
                    builder.Append("**");
                    break;
                case "i":
                case "em":
                    builder.Append('*');
                    this.AppendNodes(builder, element.Nodes());
                    builder.Append('*');
                    break;
                case "inheritdoc":
                    break;
                default:
                    this.AppendNodes(builder, element.Nodes());
                    break;
            }
        }

        private void AppendSee(StringBuilder builder, XElement element)
        {
            var langword = (string?)element.Attribute("langword");
            if (!string.IsNullOrEmpty(langword))
            {
                builder.Append('`').Append(langword).Append('`');
                return;
            }

            var href = (string?)element.Attribute("href");
            if (!string.IsNullOrEmpty(href))
            {
                var hrefText = Whitespace.Replace(element.Value, " ").Trim();
                builder.Append('[').Append(hrefText.Length == 0 ? href : hrefText).Append("](").Append(href).Append(')');
                return;
            }

            var cref = (string?)element.Attribute("cref");
            if (string.IsNullOrEmpty(cref))
            {
                this.AppendNodes(builder, element.Nodes());
                return;
            }

            var innerText = Whitespace.Replace(element.Value, " ").Trim();
            var display = "`" + (innerText.Length > 0 ? innerText : CrefDisplayName(cref)) + "`";
            var target = this.linkResolver?.Invoke(cref);
            if (target is null)
            {
                builder.Append(display);
            }
            else
            {
                builder.Append('[').Append(display).Append("](").Append(target).Append(')');
            }
        }

        private void AppendList(StringBuilder builder, XElement list)
        {
            builder.Append("\n\n");
            var numbered = string.Equals((string?)list.Attribute("type"), "number", StringComparison.Ordinal);
            var index = 1;

            foreach (var item in list.Elements("item"))
            {
                var term = item.Element("term");
                var description = item.Element("description");
                var line = new StringBuilder();

                if (term is not null)
                {
                    line.Append("**");
                    this.AppendNodes(line, term.Nodes());
                    line.Append("**");
                    if (description is not null)
                    {
                        line.Append(" - ");
                    }
                }

                if (description is not null)
                {
                    this.AppendNodes(line, description.Nodes());
                }

                if (term is null && description is null)
                {
                    this.AppendNodes(line, item.Nodes());
                }

                builder.Append(numbered ? index + ". " : "- ").Append(Whitespace.Replace(line.ToString(), " ").Trim()).Append('\n');
                index++;
            }

            builder.Append('\n');
        }

        private static string Normalize(string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var result = new StringBuilder();
            var inCode = false;

            foreach (var rawLine in lines)
            {
                var line = inCode ? rawLine.TrimEnd() : rawLine.Trim();
                if (line.StartsWith("```", StringComparison.Ordinal))
                {
                    inCode = !inCode;
                }

                result.Append(line).Append('\n');
            }

            return BlankLines.Replace(result.ToString(), "\n\n").Trim('\n', ' ');
        }

        private static string Dedent(string code)
        {
            var lines = code.Replace("\r\n", "\n").Split('\n').ToList();
            while (lines.Count > 0 && lines[0].Trim().Length == 0)
            {
                lines.RemoveAt(0);
            }

            while (lines.Count > 0 && lines[^1].Trim().Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            var indent = lines.Where(line => line.Trim().Length > 0)
                .Select(line => line.Length - line.TrimStart().Length)
                .DefaultIfEmpty(0)
                .Min();

            return string.Join("\n", lines.Select(line => line.Length >= indent ? line[indent..].TrimEnd() : line.Trim()));
        }

        private static List<string> SplitOutsideBraces(string body)
        {
            var parts = new List<string>();
            var depth = 0;
            var current = new StringBuilder();

            foreach (var character in body)
            {
                if (character == '{')
                {
                    depth++;
                }
                else if (character == '}')
                {
                    depth--;
                }

                if (character == '.' && depth == 0)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(character);
            }

            parts.Add(current.ToString());
            return parts;
        }

        private static string ReadableArity(string part)
        {
            var tick = part.IndexOf('`', StringComparison.Ordinal);
            if (tick < 0)
            {
                return part.Replace('{', '<').Replace('}', '>');
            }

            var name = part[..tick];
            var arityText = part[tick..].TrimStart('`');
            if (!int.TryParse(arityText, NumberStyles.None, CultureInfo.InvariantCulture, out var arity) || arity <= 0)
            {
                return name;
            }

            var parameters = arity == 1 ? "T" : string.Join(", ", Enumerable.Range(1, arity).Select(i => "T" + i.ToString(CultureInfo.InvariantCulture)));
            return name + "<" + parameters + ">";
        }
    }
}
