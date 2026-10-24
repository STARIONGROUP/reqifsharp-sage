// -------------------------------------------------------------------------------------------------
// <copyright file="Markdown.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.MetamodelGen.Rendering
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Small markdown helpers shared by the page renderers.
    /// </summary>
    public static class Markdown
    {
        /// <summary>
        /// Escapes text for use inside a table cell: pipes are escaped and line breaks become spaces.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The escaped text.</returns>
        public static string Cell(string? text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : text.Replace("\r", string.Empty).Replace("\n", " ").Replace("|", "\\|").Trim();
        }

        /// <summary>
        /// Quotes a value for a YAML front-matter line.
        /// </summary>
        /// <param name="value">The value, or null.</param>
        /// <returns>The quoted value, or <c>null</c>.</returns>
        public static string Yaml(string? value)
        {
            return value is null ? "null" : "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        /// <summary>
        /// Renders a link to another element page in the same directory.
        /// </summary>
        /// <param name="name">The element name.</param>
        /// <returns>The markdown link.</returns>
        public static string Link(string name)
        {
            return $"[{name}]({name}.md)";
        }

        /// <summary>
        /// Renders a bulleted list of links, or <c>_None._</c>.
        /// </summary>
        /// <param name="names">The element names.</param>
        /// <returns>The list.</returns>
        public static string LinkList(IEnumerable<string> names)
        {
            var items = names.Select(n => "- " + Link(n)).ToList();
            return items.Count == 0 ? "_None._" : string.Join("\n", items);
        }

        /// <summary>
        /// Turns a documentation body into markdown paragraphs.
        /// </summary>
        /// <param name="documentation">The documentation, or null.</param>
        /// <returns>The paragraphs, or a placeholder.</returns>
        public static string Paragraphs(string? documentation)
        {
            return string.IsNullOrWhiteSpace(documentation) ? "_No description in the metamodel._" : documentation.Trim();
        }
    }
}
