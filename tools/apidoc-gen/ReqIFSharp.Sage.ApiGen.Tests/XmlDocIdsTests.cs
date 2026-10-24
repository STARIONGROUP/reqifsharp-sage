// -------------------------------------------------------------------------------------------------
// <copyright file="XmlDocIdsTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Schema;

    using ReqIFSharp.Sage.ApiGen;
    using ReqIFSharp.Sage.ApiGen.Documentation;

    [TestFixture]
    public class XmlDocIdsTests
    {
        [Test]
        public void Type_ids_use_dots_for_nesting_and_keep_generic_arity()
        {
            Assert.Multiple(() =>
            {
                Assert.That(XmlDocIds.ForType(typeof(SpecObject)), Is.EqualTo("T:ReqIFSharp.SpecObject"));
                Assert.That(XmlDocIds.ForType(typeof(List<>)), Is.EqualTo("T:System.Collections.Generic.List`1"));
                Assert.That(XmlDocIds.ForType(typeof(Dictionary<,>.KeyCollection)), Is.EqualTo("T:System.Collections.Generic.Dictionary`2.KeyCollection"));
            });
        }

        [Test]
        public void Method_ids_distinguish_overloads_by_parameter_types()
        {
            var overloads = typeof(ReqIFDeserializer).GetMethods().Where(method => method.Name == "Deserialize").Select(XmlDocIds.ForMethod).OrderBy(id => id, StringComparer.Ordinal).ToList();

            Assert.That(overloads, Is.EqualTo(new[]
            {
                "M:ReqIFSharp.ReqIFDeserializer.Deserialize(System.IO.Stream,ReqIFSharp.SupportedFileExtensionKind,System.Boolean,System.Xml.Schema.ValidationEventHandler)",
                "M:ReqIFSharp.ReqIFDeserializer.Deserialize(System.String,System.Boolean,System.Xml.Schema.ValidationEventHandler)",
            }));
        }

        [Test]
        public void Method_ids_encode_generic_parameters_by_ref_arrays_and_constructors()
        {
            var select = typeof(Enumerable).GetMethods().Single(method => method.Name == "Select" && method.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2);
            var add = typeof(List<>).GetMethod("Add")!;
            var tryParse = typeof(int).GetMethod("TryParse", new[] { typeof(string), typeof(int).MakeByRefType() })!;
            var join = typeof(string).GetMethod("Join", new[] { typeof(string), typeof(string[]) })!;
            var constructor = typeof(ReqIFDeserializer).GetConstructors().Single();
            var parameterless = typeof(SpecObject).GetConstructor(Type.EmptyTypes)!;

            Assert.Multiple(() =>
            {
                Assert.That(XmlDocIds.ForMethod(select), Is.EqualTo("M:System.Linq.Enumerable.Select``2(System.Collections.Generic.IEnumerable{``0},System.Func{``0,``1})"));
                Assert.That(XmlDocIds.ForMethod(add), Is.EqualTo("M:System.Collections.Generic.List`1.Add(`0)"));
                Assert.That(XmlDocIds.ForMethod(tryParse), Is.EqualTo("M:System.Int32.TryParse(System.String,System.Int32@)"));
                Assert.That(XmlDocIds.ForMethod(join), Is.EqualTo("M:System.String.Join(System.String,System.String[])"));
                Assert.That(XmlDocIds.ForMethod(constructor), Is.EqualTo("M:ReqIFSharp.ReqIFDeserializer.#ctor(Microsoft.Extensions.Logging.ILoggerFactory)"));
                Assert.That(XmlDocIds.ForMethod(parameterless), Is.EqualTo("M:ReqIFSharp.SpecObject.#ctor"));
            });
        }

        [Test]
        public void Nested_generic_parameter_types_are_written_with_braces_per_nesting_level()
        {
            Assert.That(
                XmlDocIds.ParameterTypeName(typeof(Dictionary<string, int>.KeyCollection)),
                Is.EqualTo("System.Collections.Generic.Dictionary{System.String,System.Int32}.KeyCollection"));
        }

        [Test]
        public void Property_ids_have_the_property_prefix()
        {
            Assert.That(XmlDocIds.ForProperty(typeof(Identifiable).GetProperty("LongName")!), Is.EqualTo("P:ReqIFSharp.Identifiable.LongName"));
        }

        [Test]
        public void Generated_ids_are_found_in_the_shipped_xml_documentation()
        {
            var source = ApiGenerator.SourceFor(typeof(ReqIF).Assembly, ApiGenerator.ReqIFSharpPackageId);
            Assert.That(source.XmlDocumentationPath, Is.Not.Null.And.Matches<string>(File.Exists));

            var set = new XmlDocumentationSet();
            set.AddFile(source.XmlDocumentationPath!);

            var deserialize = typeof(ReqIFDeserializer).GetMethod("Deserialize", new[] { typeof(string), typeof(bool), typeof(ValidationEventHandler) })!;

            Assert.Multiple(() =>
            {
                Assert.That(set.Find(XmlDocIds.ForType(typeof(SpecObject))), Is.Not.Null);
                Assert.That(set.Find(XmlDocIds.ForMethod(deserialize)), Is.Not.Null);
                Assert.That(set.Find(XmlDocIds.ForProperty(typeof(Identifiable).GetProperty("LongName")!)), Is.Not.Null);
            });
        }
    }
}
