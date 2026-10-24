// -------------------------------------------------------------------------------------------------
// <copyright file="ApiModelBuilderTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.ApiGen.Tests
{
    using System;
    using System.Linq;

    using ReqIFSharp.Sage.ApiGen;

    [TestFixture]
    public class ApiModelBuilderTests
    {
        [Test]
        public void Package_versions_come_from_the_referenced_assemblies()
        {
            var packages = TestModel.Instance.Packages;

            Assert.Multiple(() =>
            {
                Assert.That(packages.Select(package => package.PackageId), Is.EqualTo(new[] { "ReqIFSharp", "ReqIFSharp.Extensions" }));
                Assert.That(packages[0].Version, Is.EqualTo("10.1.2"));
                Assert.That(packages[1].Version, Is.EqualTo("5.2.2"));
                Assert.That(packages.Select(package => package.Version), Has.None.Contains("+"));
            });
        }

        [Test]
        public void Types_are_ordered_ordinally_by_full_name_and_documented()
        {
            var names = TestModel.Instance.Types.Select(type => type.FullName).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(names, Is.EqualTo(names.OrderBy(name => name, StringComparer.Ordinal).ToList()));
                Assert.That(names, Does.Contain("ReqIFSharp.SpecObject").And.Contain("ReqIFSharp.Extensions.Services.ReqIFLoaderService"));
                Assert.That(TestModel.Instance.Warnings, Is.Empty);
            });
        }

        [Test]
        public void ReqIFDeserializer_deserialize_overloads_have_csharp_signatures()
        {
            var deserializer = TestModel.Type("ReqIFSharp.ReqIFDeserializer");
            var signatures = deserializer.Methods.Where(method => method.Name.StartsWith("Deserialize", StringComparison.Ordinal)).Select(method => method.Signature).ToList();

            Assert.That(signatures, Is.EqualTo(new[]
            {
                "public IEnumerable<ReqIF> Deserialize(string fileUri, bool validate = false, ValidationEventHandler validationEventHandler = null)",
                "public IEnumerable<ReqIF> Deserialize(Stream stream, SupportedFileExtensionKind fileExtensionKind, bool validate = false, ValidationEventHandler validationEventHandler = null)",
                "public Task<IEnumerable<ReqIF>> DeserializeAsync(string fileUri, CancellationToken token, bool validate = false, ValidationEventHandler validationEventHandler = null)",
                "public Task<IEnumerable<ReqIF>> DeserializeAsync(Stream stream, SupportedFileExtensionKind fileExtensionKind, CancellationToken token, bool validate = false, ValidationEventHandler validationEventHandler = null)",
            }));
        }

        [Test]
        public void Deserialize_documentation_includes_summary_parameters_and_returns()
        {
            var deserialize = TestModel.Type("ReqIFSharp.ReqIFDeserializer").Methods.First(method => method.Name == "Deserialize");

            Assert.Multiple(() =>
            {
                Assert.That(deserialize.Doc.Summary, Is.Not.Null);
                Assert.That(deserialize.Doc.Parameters.Keys, Does.Contain("fileUri"));
                Assert.That(deserialize.Doc.Returns, Is.Not.Null);
                Assert.That(deserialize.Parameters.Select(parameter => parameter.DefaultValue), Is.EqualTo(new[] { null, "false", "null" }));
            });
        }

        [Test]
        public void SpecObject_maps_to_the_SpecObject_metaclass_and_SPEC_OBJECT_element()
        {
            var specObject = TestModel.Type("ReqIFSharp.SpecObject");
            var type = specObject.Properties.Single(property => property.Name == "Type");

            Assert.Multiple(() =>
            {
                Assert.That(specObject.ReqIfClass, Is.EqualTo("SpecObject"));
                Assert.That(specObject.XmlElement, Is.EqualTo("SPEC-OBJECT"));
                Assert.That(specObject.IsAbstract, Is.False);
                Assert.That(type.ReqIf, Is.Not.Null);
                Assert.That(type.ReqIf!.Feature, Is.EqualTo("type"));
                Assert.That(type.ReqIf.Multiplicity, Is.EqualTo("1"));
            });
        }

        [TestCase("ReqIFSharp.Identifiable", "Identifiable")]
        [TestCase("ReqIFSharp.SpecType", "SpecType")]
        [TestCase("ReqIFSharp.AttributeDefinition", "AttributeDefinition")]
        public void Abstract_metaclasses_have_no_xml_element(string fullName, string reqIfClass)
        {
            var type = TestModel.Type(fullName);

            Assert.Multiple(() =>
            {
                Assert.That(type.ReqIfClass, Is.EqualTo(reqIfClass));
                Assert.That(type.IsAbstract, Is.True);
                Assert.That(type.XmlElement, Is.Null);
            });
        }

        [Test]
        public void Types_without_the_reqif_class_attribute_are_not_mapped()
        {
            var deserializer = TestModel.Type("ReqIFSharp.ReqIFDeserializer");

            Assert.Multiple(() =>
            {
                Assert.That(deserializer.ReqIfClass, Is.Null);
                Assert.That(deserializer.XmlElement, Is.Null);
            });
        }

        [Test]
        public void Extension_methods_are_marked_and_listed_on_the_extended_type_and_its_subclasses()
        {
            var extensions = TestModel.Type("ReqIFSharp.Extensions.ReqIFExtensions.SpecObjectExtensions");
            var queryContainer = extensions.Methods.Single(method => method.Name == "QueryContainerSpecifications");
            var specObject = TestModel.Type("ReqIFSharp.SpecObject");
            var available = specObject.AvailableExtensionMethods.Select(method => method.Name).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(extensions.IsStatic, Is.True);
                Assert.That(queryContainer.IsExtension, Is.True);
                Assert.That(queryContainer.ExtendedType, Is.EqualTo(typeof(SpecObject)));
                Assert.That(queryContainer.Signature, Does.Contain("(this SpecObject specObject)"));
                Assert.That(available, Does.Contain("QueryContainerSpecifications"));
                Assert.That(available, Does.Contain("QuerySpecRelations"));

                // Declared on base types: Identifiable and SpecElementWithAttributes.
                Assert.That(available, Does.Contain("QueryTypeDisplayName"));
                Assert.That(available, Does.Contain("QueryExternalObjects"));
                Assert.That(available, Does.Not.Contain("QueryAllContainedSpecObjects"));
            });
        }

        [Test]
        public void Enum_literals_carry_their_values_in_value_order()
        {
            var kind = TestModel.Type("ReqIFSharp.SupportedFileExtensionKind");

            Assert.Multiple(() =>
            {
                Assert.That(kind.Kind, Is.EqualTo("enum"));
                Assert.That(kind.Interfaces, Is.Empty, "runtime-implied enum interfaces are left out");
                Assert.That(kind.Fields.Select(field => field.Name + "=" + field.Value), Is.EqualTo(new[] { "Reqif=0", "Reqifz=1" }));
            });
        }

        [Test]
        public void Derived_types_and_inheritance_chain_are_linked()
        {
            var specObject = TestModel.Type("ReqIFSharp.SpecObject");
            var specElement = TestModel.Type("ReqIFSharp.SpecElementWithAttributes");

            Assert.Multiple(() =>
            {
                Assert.That(specObject.InheritanceChain.Select(type => type.Name), Is.EqualTo(new[] { "Identifiable", "SpecElementWithAttributes" }));
                Assert.That(specObject.BaseType, Is.EqualTo(typeof(SpecElementWithAttributes)));
                Assert.That(specElement.DerivedTypes.Select(type => type.Name), Does.Contain("SpecObject"));
            });
        }
    }
}
