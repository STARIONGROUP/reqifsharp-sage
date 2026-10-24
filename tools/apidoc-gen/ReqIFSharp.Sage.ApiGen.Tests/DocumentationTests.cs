// -------------------------------------------------------------------------------------------------
// <copyright file="DocumentationTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Tests
{
    using System.IO;
    using System.Threading;
    using System.Xml.Linq;
    using System.Xml.Schema;

    using ReqIFSharp.Sage.ApiGen.Documentation;

    [TestFixture]
    public class DocumentationTests
    {
        private const string InterfaceDeserialize = "M:ReqIFSharp.IReqIFDeSerializer.Deserialize(System.String,System.Boolean,System.Xml.Schema.ValidationEventHandler)";

        private const string ClassDeserialize = "M:ReqIFSharp.ReqIFDeserializer.Deserialize(System.String,System.Boolean,System.Xml.Schema.ValidationEventHandler)";

        private const string InterfaceDeserializeAsync = "M:ReqIFSharp.IReqIFDeSerializer.DeserializeAsync(System.String,System.Threading.CancellationToken,System.Boolean,System.Xml.Schema.ValidationEventHandler)";

        private static XmlDocumentationSet SyntheticDocumentation()
        {
            var document = XDocument.Parse($"""
                <doc>
                  <members>
                    <member name="{InterfaceDeserialize}">
                      <summary>Deserializes the <see cref="T:ReqIFSharp.ReqIF"/> documents at <paramref name="fileUri"/>.</summary>
                      <param name="fileUri">The path of the file.</param>
                      <returns>The documents, or <see langword="null"/>.</returns>
                      <exception cref="T:System.ArgumentException">When <paramref name="fileUri"/> is empty.</exception>
                    </member>
                    <member name="{ClassDeserialize}">
                      <inheritdoc/>
                      <remarks>Class-specific remarks.</remarks>
                    </member>
                    <member name="{InterfaceDeserializeAsync}">
                      <summary>Asynchronously deserializes.</summary>
                    </member>
                    <member name="T:ReqIFSharp.Identifiable">
                      <summary>The identifiable base.</summary>
                    </member>
                    <member name="T:ReqIFSharp.SpecObject">
                      <inheritdoc cref="T:ReqIFSharp.Identifiable"/>
                    </member>
                  </members>
                </doc>
                """);

            var set = new XmlDocumentationSet();
            set.Add(document);
            return set;
        }

        [Test]
        public void Inheritdoc_takes_missing_parts_from_the_implemented_interface_member()
        {
            var resolver = new DocumentationResolver(SyntheticDocumentation());
            var method = typeof(ReqIFDeserializer).GetMethod("Deserialize", new[] { typeof(string), typeof(bool), typeof(ValidationEventHandler) })!;

            var doc = resolver.Resolve(method);
            var renderer = new DocMarkdownRenderer();

            Assert.Multiple(() =>
            {
                Assert.That(renderer.Render(doc.Summary), Is.EqualTo("Deserializes the `ReqIF` documents at `fileUri`."));
                Assert.That(renderer.Render(doc.Remarks), Is.EqualTo("Class-specific remarks."));
                Assert.That(renderer.Render(doc.Returns), Is.EqualTo("The documents, or `null`."));
                Assert.That(doc.Parameters.ContainsKey("fileUri"), Is.True);
                Assert.That(doc.Exceptions, Has.Count.EqualTo(1));
                Assert.That(doc.InheritedFrom, Is.EqualTo(InterfaceDeserialize));
            });
        }

        [Test]
        public void An_undocumented_interface_implementation_inherits_implicitly()
        {
            var resolver = new DocumentationResolver(SyntheticDocumentation());
            var method = typeof(ReqIFDeserializer).GetMethod("DeserializeAsync", new[] { typeof(string), typeof(CancellationToken), typeof(bool), typeof(ValidationEventHandler) })!;

            var doc = resolver.Resolve(method);

            Assert.That(new DocMarkdownRenderer().Render(doc.Summary), Is.EqualTo("Asynchronously deserializes."));
        }

        [Test]
        public void Inheritdoc_with_a_cref_follows_the_cref()
        {
            var resolver = new DocumentationResolver(SyntheticDocumentation());

            var doc = resolver.Resolve(typeof(SpecObject));

            Assert.Multiple(() =>
            {
                Assert.That(new DocMarkdownRenderer().Render(doc.Summary), Is.EqualTo("The identifiable base."));
                Assert.That(doc.InheritedFrom, Is.EqualTo("T:ReqIFSharp.Identifiable"));
            });
        }

        [Test]
        public void An_undocumented_type_does_not_inherit_its_base_type_documentation()
        {
            var resolver = new DocumentationResolver(SyntheticDocumentation());

            Assert.That(resolver.Resolve(typeof(SpecRelation)).IsEmpty, Is.True);
        }

        [Test]
        public void Renderer_converts_inline_tags_to_markdown()
        {
            var summary = XElement.Parse("""
                <summary>
                  Reads a <see cref="T:ReqIFSharp.SpecObject"/> using
                  <see cref="M:ReqIFSharp.ReqIFDeserializer.Deserialize(System.String,System.Boolean,System.Xml.Schema.ValidationEventHandler)"/>;
                  returns <see langword="true"/> when <c>validate</c> is set, see <see href="https://www.omg.org/spec/ReqIF">the spec</see>
                  and <see cref="T:System.Collections.Generic.IEnumerable`1"/>.
                  <para>Second paragraph with <typeparamref name="T"/>.</para>
                </summary>
                """);

            var rendered = new DocMarkdownRenderer().Render(summary);

            Assert.That(rendered, Is.EqualTo(
                "Reads a `SpecObject` using `ReqIFDeserializer.Deserialize`; returns `true` when `validate` is set, see [the spec](https://www.omg.org/spec/ReqIF) and `IEnumerable<T>`.\n\nSecond paragraph with `T`."));
        }

        [Test]
        public void Renderer_links_crefs_through_the_link_resolver()
        {
            var summary = XElement.Parse("<summary>A <see cref=\"T:ReqIFSharp.SpecObject\"/> or <see cref=\"T:System.String\"/>.</summary>");
            var renderer = new DocMarkdownRenderer(cref => cref == "T:ReqIFSharp.SpecObject" ? "ReqIFSharp.SpecObject.md" : null);

            Assert.That(renderer.Render(summary), Is.EqualTo("A [`SpecObject`](ReqIFSharp.SpecObject.md) or `String`."));
        }

        [Test]
        public void Renderer_keeps_code_blocks_and_lists()
        {
            var remarks = XElement.Parse("""
                <remarks>
                  Usage:
                  <code>
                  var deserializer = new ReqIFDeserializer();
                  var reqifs = deserializer.Deserialize("file.reqif");
                  </code>
                  <list type="bullet">
                    <item><term>reqif</term><description>plain XML</description></item>
                    <item><description>zip | archive</description></item>
                  </list>
                </remarks>
                """);

            var renderer = new DocMarkdownRenderer();

            Assert.Multiple(() =>
            {
                Assert.That(renderer.Render(remarks), Is.EqualTo(
                    "Usage:\n\n```csharp\nvar deserializer = new ReqIFDeserializer();\nvar reqifs = deserializer.Deserialize(\"file.reqif\");\n```\n\n- **reqif** - plain XML\n- zip | archive"));
                Assert.That(renderer.RenderInline(remarks), Does.Contain("zip \\| archive").And.Not.Contain("\n"));
            });
        }

        [Test]
        public void Cref_display_names_drop_namespaces_and_parameters()
        {
            Assert.Multiple(() =>
            {
                Assert.That(DocMarkdownRenderer.CrefDisplayName("T:ReqIFSharp.SpecObject"), Is.EqualTo("SpecObject"));
                Assert.That(DocMarkdownRenderer.CrefDisplayName("P:ReqIFSharp.Identifiable.LongName"), Is.EqualTo("Identifiable.LongName"));
                Assert.That(DocMarkdownRenderer.CrefDisplayName("M:ReqIFSharp.SpecObject.#ctor(Microsoft.Extensions.Logging.ILoggerFactory)"), Is.EqualTo("SpecObject.ctor"));
                Assert.That(DocMarkdownRenderer.CrefDisplayName("T:System.Collections.Generic.Dictionary`2"), Is.EqualTo("Dictionary<T1, T2>"));
            });
        }

        [Test]
        public void Missing_xml_documentation_still_generates_with_a_warning()
        {
            var source = new Model.ApiAssemblySource(typeof(ReqIF).Assembly, ApiGenerator.ReqIFSharpPackageId, Path.Combine(Path.GetTempPath(), "does-not-exist", "ReqIFSharp.xml"));

            var model = ApiModelBuilder.Build(new[] { source });

            Assert.Multiple(() =>
            {
                Assert.That(model.Types, Is.Not.Empty);
                Assert.That(model.Warnings, Has.Some.Contains("XML documentation file for ReqIFSharp not found"));
            });
        }
    }
}
