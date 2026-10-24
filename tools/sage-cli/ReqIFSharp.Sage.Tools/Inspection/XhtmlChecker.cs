// -------------------------------------------------------------------------------------------------
// <copyright file="XhtmlChecker.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Inspection
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// The ReqIF 1.2 XHTML rules R-027 to R-034, plus the XHTML-related Implementation Guide rules G-008 to G-011
    /// (reported only when the context includes guide rules).
    /// </summary>
    internal sealed class XhtmlChecker
    {
        /// <summary>
        /// The XHTML elements of the modules the ReqIF XHTML driver (driver.xsd) includes: text, hypertext, list, edit,
        /// presentation, style attribute, object and (basic) tables.
        /// </summary>
        internal static readonly IReadOnlySet<string> AllowedElements = new HashSet<string>(StringComparer.Ordinal)
        {
            "a", "abbr", "acronym", "address", "b", "big", "blockquote", "br", "caption", "cite", "code", "col", "colgroup",
            "dd", "del", "dfn", "div", "dl", "dt", "em", "h1", "h2", "h3", "h4", "h5", "h6", "hr", "i", "ins", "kbd", "li",
            "object", "ol", "p", "param", "pre", "q", "samp", "small", "span", "strong", "sub", "sup", "table", "tbody",
            "td", "tfoot", "th", "thead", "tr", "tt", "ul", "var",
        };

        private static readonly HashSet<string> ObjectAttributes = new(StringComparer.Ordinal) { "data", "type", "width", "height" };

        private const string Png = "image/png";

        private readonly ReqIFXmlDocument document;

        private readonly InspectionContext context;

        /// <summary>
        /// Initializes a new instance of the <see cref="XhtmlChecker"/> class.
        /// </summary>
        /// <param name="document">The document to check.</param>
        /// <param name="context">The context to report to.</param>
        internal XhtmlChecker(ReqIFXmlDocument document, InspectionContext context)
        {
            this.document = document;
            this.context = context;
        }

        /// <summary>
        /// Runs every rule on every ATTRIBUTE-VALUE-XHTML (element values and default values).
        /// </summary>
        internal void Run()
        {
            foreach (var value in this.document.AttributeValues.Where(value => value.Name.LocalName == "ATTRIBUTE-VALUE-XHTML"))
            {
                var owner = SemanticChecker.IsDefaultValue(value) ? value.Parent!.Parent! : value.Parent?.Parent ?? value;
                var definition = SemanticChecker.EffectiveDefinition(this.document, value);
                var subject = SemanticChecker.IsDefaultValue(value)
                    ? $"The DEFAULT-VALUE of {ReqIFXml.Name(owner)}"
                    : definition is null
                        ? $"An XHTML value of {ReqIFXml.Name(owner)}"
                        : $"The {ReqIFXml.Name(definition)} value of {ReqIFXml.Name(owner)}";

                var theValue = ReqIFXml.Child(value, "THE-VALUE");
                var original = ReqIFXml.Child(value, "THE-ORIGINAL-VALUE");

                if (theValue is not null)
                {
                    this.CheckContent(theValue, subject);
                }

                if (original is not null)
                {
                    this.CheckContent(original, subject + " (THE-ORIGINAL-VALUE)");
                }

                var simplified = ReqIFXml.Attribute(value, "IS-SIMPLIFIED");

                if ((simplified == "true" || simplified == "1") && original is null)
                {
                    this.context.Report(
                        RuleIds.XhtmlSimplifiedWithoutOriginal,
                        $"{subject} is marked IS-SIMPLIFIED but has no THE-ORIGINAL-VALUE, so the original formatting is lost.",
                        value);
                }
            }
        }

        private static string ResolveRelative(string data)
        {
            var path = data;
            var cut = path.IndexOfAny(['#', '?']);

            if (cut >= 0)
            {
                path = path[..cut];
            }

            try
            {
                return Uri.UnescapeDataString(path);
            }
            catch (UriFormatException)
            {
                return path;
            }
        }

        private static bool IsAbsolute(string data) =>
            data.StartsWith('/') || data.StartsWith('\\') || (Uri.TryCreate(data, UriKind.Absolute, out var uri) && uri.Scheme.Length > 1);

        private static bool HasText(XElement element) => element.DescendantNodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value));

        private void CheckContent(XElement container, string subject)
        {
            var foreignText = container.Nodes().OfType<XText>().Any(text => !string.IsNullOrWhiteSpace(text.Value));
            var foreignElements = container.Descendants().Where(element => element.Name.NamespaceName != ReqIFXml.XhtmlNamespace).Select(element => element.Name.ToString()).Distinct().ToList();

            if (foreignText || foreignElements.Count > 0)
            {
                var what = foreignText ? "text directly inside " + container.Name.LocalName : string.Empty;

                if (foreignElements.Count > 0)
                {
                    what += (what.Length > 0 ? " and " : string.Empty) + $"elements outside the XHTML namespace ({string.Join(", ", foreignElements.Take(5))})";
                }

                this.context.Report(
                    RuleIds.XhtmlNamespace,
                    $"{subject} has {what}; the content must be XHTML elements in the namespace {ReqIFXml.XhtmlNamespace}, wrapped in a block element such as xhtml:div or xhtml:p.",
                    container);
            }

            var xhtmlElements = container.Descendants().Where(element => element.Name.NamespaceName == ReqIFXml.XhtmlNamespace).ToList();

            foreach (var group in xhtmlElements.Where(element => !AllowedElements.Contains(element.Name.LocalName)).GroupBy(element => element.Name.LocalName))
            {
                this.context.Report(
                    RuleIds.XhtmlElementNotAllowed,
                    $"{subject} uses the XHTML element <{group.Key}> ({group.Count()}x), which is not part of the XHTML modules ReqIF allows; importing tools may drop or reject it.",
                    group.First(),
                    identifier: ReqIFXml.OwnerIdentifier(container));
            }

            var withClass = xhtmlElements.FirstOrDefault(element => element.Attribute("class") is not null);

            if (withClass is not null)
            {
                this.context.Report(
                    RuleIds.XhtmlClassAttribute,
                    $"{subject} uses the class attribute (first on <{withClass.Name.LocalName}>), which ReqIF does not allow in XHTML content; use the style attribute instead.",
                    withClass,
                    identifier: ReqIFXml.OwnerIdentifier(container));
            }

            foreach (var objectElement in xhtmlElements.Where(element => element.Name.LocalName == "object"))
            {
                this.CheckObject(objectElement, container, subject);
            }

            var heading = xhtmlElements.FirstOrDefault(element => element.Name.LocalName is "h1" or "h2" or "h3" or "h4" or "h5" or "h6");

            if (heading is not null)
            {
                this.context.Report(
                    RuleIds.XhtmlHeadings,
                    $"{subject} uses the heading element <{heading.Name.LocalName}>; the guide recommends holding headline text in ReqIF.ChapterName and expressing its level by SpecHierarchy nesting.",
                    heading,
                    identifier: ReqIFXml.OwnerIdentifier(container));
            }

            var margin = xhtmlElements.FirstOrDefault(element => element.Attribute("style")?.Value.Contains("margin-left", StringComparison.OrdinalIgnoreCase) == true);

            if (margin is not null)
            {
                this.context.Report(
                    RuleIds.XhtmlMarginIndentation,
                    $"{subject} indents with a margin-left style (on <{margin.Name.LocalName}>), which importing tools need not support; the guide recommends <blockquote>.",
                    margin,
                    identifier: ReqIFXml.OwnerIdentifier(container));
            }
        }

        private void CheckObject(XElement objectElement, XElement container, string subject)
        {
            var identifier = ReqIFXml.OwnerIdentifier(container);
            var data = objectElement.Attribute("data")?.Value.Trim();
            var type = objectElement.Attribute("type")?.Value.Trim();
            var isAlternative = objectElement.Parent is { } parent && parent.Name == objectElement.Name;
            var label = string.IsNullOrEmpty(data) ? "<object>" : $"<object data=\"{data}\">";

            if (string.IsNullOrEmpty(data))
            {
                this.context.Report(
                    RuleIds.XhtmlObjectData,
                    $"{subject} has an <object> without a data attribute, so the location of the embedded object is unknown.",
                    objectElement,
                    identifier: identifier);
            }

            if (string.IsNullOrEmpty(type))
            {
                this.context.Report(
                    RuleIds.ObjectMimeType,
                    $"{subject} has an {label} without a type attribute; embedded objects should always carry a MIME type (application/octet-stream when unknown).",
                    objectElement,
                    identifier: identifier);
            }
            else
            {
                var extra = objectElement.Attributes()
                    .Where(attribute => !attribute.IsNamespaceDeclaration && attribute.Name.NamespaceName.Length == 0 && !ObjectAttributes.Contains(attribute.Name.LocalName))
                    .Select(attribute => attribute.Name.LocalName)
                    .ToList();

                if (extra.Count > 0)
                {
                    this.context.Report(
                        RuleIds.XhtmlObjectAttributes,
                        $"{subject} has an {label} of type {type} that also uses {string.Join(", ", extra)}; only data, type, width and height should be used.",
                        objectElement,
                        identifier: identifier);
                }
            }

            if (isAlternative)
            {
                if (!string.IsNullOrEmpty(type) && !string.Equals(type, Png, StringComparison.OrdinalIgnoreCase))
                {
                    this.context.Report(
                        RuleIds.XhtmlObjectAlternative,
                        $"{subject} has an alternative image {label} of type {type}; alternative images must be {Png}.",
                        objectElement,
                        identifier: identifier);
                }

                if (objectElement.Elements().Any(child => child.Name.NamespaceName == ReqIFXml.XhtmlNamespace && child.Name.LocalName != "object"))
                {
                    this.context.Report(
                        RuleIds.ObjectAlternativeTextFormatted,
                        $"{subject} has an alternative image {label} whose alternative text is formatted XHTML; plain text is recommended.",
                        objectElement,
                        identifier: identifier);
                }
            }
            else if (!string.IsNullOrEmpty(type) && !string.Equals(type, Png, StringComparison.OrdinalIgnoreCase))
            {
                var alternative = objectElement.Elements().FirstOrDefault(child => child.Name == objectElement.Name);

                if (alternative is null || !HasText(objectElement))
                {
                    this.context.Report(
                        RuleIds.XhtmlObjectAlternative,
                        $"{subject} has an {label} of type {type} without {(alternative is null ? "a nested image/png alternative <object> and alternative text" : "alternative text")}; tools that cannot open the object have nothing to show.",
                        objectElement,
                        identifier: identifier);
                }
            }

            if (!string.IsNullOrEmpty(data) && !IsAbsolute(data))
            {
                var relative = ResolveRelative(data);

                if (relative.Length > 0 && !this.document.Source.AttachmentExists(relative))
                {
                    this.context.Report(
                        RuleIds.XhtmlObjectFileMissing,
                        $"{subject} has an {label} whose file '{relative}' is not in {this.document.Source.AttachmentLocation}.",
                        objectElement,
                        identifier: identifier);
                }
            }
        }
    }
}
