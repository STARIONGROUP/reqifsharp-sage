// -------------------------------------------------------------------------------------------------
// <copyright file="ApiModelBuilder.cs" company="Starion Group S.A.">
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
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    using ReqIFSharp.Sage.ApiGen.Documentation;
    using ReqIFSharp.Sage.ApiGen.Model;
    using ReqIFSharp.Sage.ApiGen.Rendering;

    /// <summary>
    /// Reflects over the public surface of the documented assemblies and builds the <see cref="ApiModel"/>:
    /// every public (including public nested) type with its public constructors, properties, methods, events
    /// and fields, the resolved XML documentation, the ReqIF metaclass mapping read from the library's
    /// <c>ReqIfClass</c> / <c>ReqIfProperty</c> attributes (matched by name through
    /// <see cref="CustomAttributeData"/>), derived types, and the extension methods applicable to each type.
    /// </summary>
    public static class ApiModelBuilder
    {
        /// <summary>
        /// The simple name of the library attribute that marks a class as a ReqIF metaclass.
        /// </summary>
        public const string ReqIfClassAttributeName = "ReqIfClassAttribute";

        /// <summary>
        /// The simple name of the library attribute that marks a property as a ReqIF metaclass feature.
        /// </summary>
        public const string ReqIfPropertyAttributeName = "ReqIfPropertyAttribute";

        private const BindingFlags DeclaredPublic = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        /// <summary>
        /// Builds the model of the given assemblies.
        /// </summary>
        /// <param name="sources">The assemblies to document, with their package ids and documentation files.</param>
        /// <returns>The model.</returns>
        public static ApiModel Build(IReadOnlyList<ApiAssemblySource> sources)
        {
            var warnings = new List<string>();
            var documentation = new XmlDocumentationSet();
            var packages = new List<ApiPackageModel>();

            foreach (var source in sources)
            {
                packages.Add(new ApiPackageModel(source.Assembly, source.PackageId, PackageVersionOf(source.Assembly), AssemblyVersionOf(source.Assembly)));

                if (string.IsNullOrEmpty(source.XmlDocumentationPath) || !File.Exists(source.XmlDocumentationPath))
                {
                    warnings.Add($"XML documentation file for {source.PackageId} not found; its types are generated without documentation.");
                    continue;
                }

                documentation.AddFile(source.XmlDocumentationPath);
            }

            var schema = sources.Select(source => ReqIfSchemaNames.FromAssembly(source.Assembly)).FirstOrDefault(names => names is not null);
            if (schema is null)
            {
                warnings.Add("No embedded reqif.xsd found; ReqIF element and feature names are not checked against the schema.");
            }

            var resolver = new DocumentationResolver(documentation);
            var types = new List<ApiTypeModel>();

            foreach (var package in packages)
            {
                foreach (var type in PublicTypesOf(package.Assembly, warnings))
                {
                    types.Add(BuildType(type, package, resolver, schema, warnings));
                }
            }

            types.Sort((left, right) => StringComparer.Ordinal.Compare(left.FullName, right.FullName));
            LinkDerivedTypes(types);
            LinkExtensionMethods(types);

            return new ApiModel(packages, types, warnings);
        }

        /// <summary>
        /// Gets the package version of an assembly: its <see cref="AssemblyInformationalVersionAttribute"/> without
        /// any <c>+commit</c> build-metadata suffix, else its <see cref="AssemblyFileVersionAttribute"/>, else its
        /// assembly version.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The version, e.g. <c>10.1.2</c>.</returns>
        public static string PackageVersionOf(Assembly assembly)
        {
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
            {
                var plus = informational.IndexOf('+', StringComparison.Ordinal);
                return (plus < 0 ? informational : informational[..plus]).Trim();
            }

            var file = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (!string.IsNullOrWhiteSpace(file))
            {
                return file.Trim();
            }

            return AssemblyVersionOf(assembly);
        }

        /// <summary>
        /// Gets the ReqIF metaclass mapping of a type from its <c>ReqIfClass</c> attribute.
        /// </summary>
        /// <remarks>
        /// The XML element name is the attribute's <c>name</c>, or the upper-kebab-case form of the metaclass name
        /// when the attribute gives none. Abstract metaclasses have no element of their own. A concrete class whose
        /// element the ReqIF schema does not declare (e.g. ReqIFSharp's <c>ExternalObject</c>) is library-specific:
        /// it is reported with a note instead of a mapping.
        /// </remarks>
        /// <param name="type">The type.</param>
        /// <param name="schema">The schema names to check against, or <see langword="null"/> to skip the check.</param>
        /// <param name="warnings">Receives a warning when the attribute's element name differs from the name derived from the class name.</param>
        /// <returns>The metaclass name, the XML element name (<see langword="null"/> for abstract metaclasses) and
        /// a note; all <see langword="null"/> when the type is not decorated.</returns>
        public static (string? ReqIfClass, string? XmlElement, string? Note) ReqIfMappingOf(Type type, ReqIfSchemaNames? schema = null, ICollection<string>? warnings = null)
        {
            var attribute = type.GetCustomAttributesData().FirstOrDefault(data => data.AttributeType.Name == ReqIfClassAttributeName);
            if (attribute is null)
            {
                return (null, null, null);
            }

            var arguments = ArgumentsOf(attribute);
            var declaredName = arguments.TryGetValue("name", out var nameValue) ? nameValue as string : null;
            var declaredAbstract = arguments.TryGetValue("isAbstract", out var abstractValue) && abstractValue is true;

            var reqIfClass = ReqIfNames.MetaclassName(SimpleName(type));
            var derivedName = ReqIfNames.ToXmlElementName(reqIfClass);

            if (!string.IsNullOrEmpty(declaredName) && !string.Equals(declaredName, derivedName, StringComparison.Ordinal))
            {
                warnings?.Add($"{type.FullName}: ReqIfClass name '{declaredName}' differs from the derived XML element name '{derivedName}'; using '{declaredName}'.");
            }

            if (declaredAbstract || type.IsAbstract)
            {
                return (reqIfClass, null, null);
            }

            var elementName = string.IsNullOrEmpty(declaredName) ? derivedName : declaredName;
            if (schema is not null && !schema.Contains(elementName))
            {
                return (null, null, $"Decorated with `[ReqIfClass(\"{elementName}\")]`, but the ReqIF schema declares no `{elementName}` element: this is a ReqIFSharp-specific class, not a ReqIF metaclass.");
            }

            return (reqIfClass, elementName, null);
        }

        /// <summary>
        /// Gets the ReqIF feature mapping of a property from its <c>ReqIfProperty</c> attribute.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="schema">The schema names the feature's XML name is checked against, or <see langword="null"/> to skip the check.</param>
        /// <returns>The mapping, or <see langword="null"/> when the property carries no such attribute.</returns>
        public static ReqIfFeatureModel? ReqIfFeatureOf(PropertyInfo property, ReqIfSchemaNames? schema = null)
        {
            var attribute = property.GetCustomAttributesData().FirstOrDefault(data => data.AttributeType.Name == ReqIfPropertyAttributeName);
            if (attribute is null)
            {
                return null;
            }

            var arguments = ArgumentsOf(attribute);

            int Int(string name, int fallback) => arguments.TryGetValue(name, out var value) && value is int number ? number : fallback;

            bool Bool(string name, bool fallback) => arguments.TryGetValue(name, out var value) && value is bool flag ? flag : fallback;

            var aggregation = "None";
            if (arguments.TryGetValue("aggregation", out var aggregationValue) && aggregationValue is not null)
            {
                aggregation = aggregationValue.ToString() ?? "None";
            }

            var feature = ReqIfNames.FeatureName(property.DeclaringType is null ? string.Empty : SimpleName(property.DeclaringType), property.Name);
            var xmlName = ReqIfNames.ToXmlElementName(feature);

            return new ReqIfFeatureModel(
                feature,
                schema is null || schema.Contains(xmlName) ? xmlName : null,
                aggregation,
                Int("lowerValue", 1),
                Int("upperValue", 1),
                Bool("isOrdered", false),
                Bool("isUnique", true),
                Bool("isReadOnly", false),
                Bool("isDerived", false),
                Bool("isDerivedUnion", false),
                arguments.TryGetValue("defaultValue", out var defaultValue) ? defaultValue as string : null);
        }

        /// <summary>
        /// Gets the C# name of a type without namespace, nested types joined with <c>.</c> and without generic arity.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The name, e.g. <c>SpecObject</c>.</returns>
        public static string SimpleName(Type type)
        {
            var name = type.Name;
            var tick = name.IndexOf('`', StringComparison.Ordinal);
            if (tick >= 0)
            {
                name = name[..tick];
            }

            return type.IsNested ? SimpleName(type.DeclaringType!) + "." + name : name;
        }

        /// <summary>
        /// Determines the kind of a type: <c>class</c>, <c>interface</c>, <c>enum</c>, <c>struct</c> or <c>delegate</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The kind.</returns>
        public static string KindOf(Type type)
        {
            if (type.IsInterface)
            {
                return "interface";
            }

            if (type.IsEnum)
            {
                return "enum";
            }

            if (type.IsValueType)
            {
                return "struct";
            }

            return typeof(Delegate).IsAssignableFrom(type) ? "delegate" : "class";
        }

        private static IEnumerable<Type> PublicTypesOf(Assembly assembly, List<string> warnings)
        {
            Type[] types;
            try
            {
                types = assembly.GetExportedTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                warnings.Add($"Some types of {assembly.GetName().Name} could not be loaded: {exception.Message}");
                types = exception.Types.Where(type => type is not null && type.IsVisible).Select(type => type!).ToArray();
            }

            return types.Where(type => !type.Name.Contains('<', StringComparison.Ordinal) && !type.IsDefined(typeof(CompilerGeneratedAttribute), false));
        }

        private static ApiTypeModel BuildType(Type type, ApiPackageModel package, DocumentationResolver resolver, ReqIfSchemaNames? schema, List<string> warnings)
        {
            var kind = KindOf(type);
            var (reqIfClass, xmlElement, reqIfNote) = ReqIfMappingOf(type, schema, warnings);

            var chain = new List<Type>();
            if (kind == "class")
            {
                for (var current = type.BaseType; current is not null && current != typeof(object); current = current.BaseType)
                {
                    chain.Insert(0, current);
                }
            }

            var constructors = kind is "class" or "struct" && !(type.IsAbstract && type.IsSealed)
                ? type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Select(constructor => BuildMethod(constructor, resolver)).ToList()
                : new List<ApiMethodModel>();

            var properties = kind is "enum" or "delegate"
                ? new List<ApiPropertyModel>()
                : type.GetProperties(DeclaredPublic)
                    .Where(property => IsPublicAccessor(property.GetMethod) || IsPublicAccessor(property.SetMethod))
                    .Select(property => BuildProperty(property, resolver, schema))
                    .ToList();

            var methods = kind is "enum" or "delegate"
                ? new List<ApiMethodModel>()
                : type.GetMethods(DeclaredPublic)
                    .Where(method => !method.IsSpecialName || method.Name.StartsWith("op_", StringComparison.Ordinal))
                    .Select(method => BuildMethod(method, resolver))
                    .ToList();

            var events = kind is "enum" or "delegate"
                ? new List<ApiEventModel>()
                : type.GetEvents(DeclaredPublic)
                    .Select(@event => new ApiEventModel(
                        @event,
                        @event.Name,
                        CSharpTypeNames.Format(@event.EventHandlerType!),
                        @event.AddMethod?.IsStatic ?? false,
                        XmlDocIds.ForMember(@event)!,
                        resolver.Resolve(@event)))
                    .ToList();

            var fields = type.GetFields(DeclaredPublic)
                .Where(field => !field.IsSpecialName)
                .Select(field => BuildField(field, type, resolver))
                .ToList();

            constructors.Sort((left, right) => CompareMethods(left, right));
            properties.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
            methods.Sort((left, right) => CompareMethods(left, right));
            events.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));

            if (type.IsEnum)
            {
                // Enumeration literals keep their declared (value) order, the order the library defines them in.
                fields = fields.OrderBy(field => Convert.ToDecimal(field.Field.GetRawConstantValue(), CultureInfo.InvariantCulture))
                    .ThenBy(field => field.Name, StringComparer.Ordinal)
                    .ToList();
            }
            else
            {
                fields.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
            }

            return new ApiTypeModel
            {
                Type = type,
                Name = SimpleName(type),
                FullName = XmlDocIds.TypeDefinitionName(type),
                Namespace = type.Namespace ?? string.Empty,
                Package = package,
                Kind = kind,
                IsAbstract = kind == "class" && type.IsAbstract && !type.IsSealed,
                IsStatic = kind == "class" && type.IsAbstract && type.IsSealed,
                IsSealed = kind == "class" && type.IsSealed && !type.IsAbstract,
                Declaration = CSharpTypeNames.TypeDeclaration(type, kind),
                BaseType = chain.Count > 0 ? chain[^1] : null,
                InheritanceChain = chain,
                // Enumerations and delegates only carry the runtime's implicit interfaces (IComparable,
                // ISpanFormattable, ...), which vary with the .NET version doing the reflection: leave them out.
                Interfaces = kind is "enum" or "delegate"
                    ? Array.Empty<Type>()
                    : type.GetInterfaces().OrderBy(i => XmlDocIds.TypeDefinitionName(i), StringComparer.Ordinal).ToList(),
                ReqIfClass = reqIfClass,
                XmlElement = xmlElement,
                ReqIfNote = reqIfNote,
                DocId = XmlDocIds.ForType(type),
                Doc = resolver.Resolve(type),
                Constructors = constructors,
                Properties = properties,
                Methods = methods,
                Events = events,
                Fields = fields,
            };
        }

        private static ApiMethodModel BuildMethod(MethodBase method, DocumentationResolver resolver)
        {
            var parameters = method.GetParameters()
                .Select(parameter => new ApiParameterModel(
                    parameter,
                    parameter.Name ?? string.Empty,
                    CSharpTypeNames.Format(parameter.ParameterType),
                    CSharpTypeNames.ParameterModifier(parameter),
                    parameter.HasDefaultValue,
                    CSharpTypeNames.DefaultValue(parameter)))
                .ToList();

            var info = method as MethodInfo;
            var isExtension = info is not null && method.IsDefined(typeof(ExtensionAttribute), false) && parameters.Count > 0;
            var isOverridable = method.IsVirtual && !method.IsFinal && method.DeclaringType is { IsInterface: false };

            return new ApiMethodModel(
                method,
                method.Name,
                CSharpTypeNames.MethodHeading(method),
                CSharpTypeNames.MethodSignature(method),
                info is null ? null : CSharpTypeNames.Format(info.ReturnType),
                method.IsStatic,
                method.IsAbstract,
                isOverridable,
                isExtension,
                isExtension ? parameters[0].Parameter.ParameterType : null,
                parameters,
                info is { IsGenericMethodDefinition: true } ? info.GetGenericArguments().Select(argument => argument.Name).ToList() : Array.Empty<string>(),
                XmlDocIds.ForMethod(method),
                resolver.Resolve(method));
        }

        private static ApiPropertyModel BuildProperty(PropertyInfo property, DocumentationResolver resolver, ReqIfSchemaNames? schema)
        {
            var accessor = property.GetMethod ?? property.SetMethod!;

            return new ApiPropertyModel(
                property,
                property.Name,
                CSharpTypeNames.Format(property.PropertyType),
                AccessorAccessibility(property.GetMethod),
                AccessorAccessibility(property.SetMethod),
                accessor.IsStatic,
                accessor.IsAbstract,
                ReqIfFeatureOf(property, schema),
                XmlDocIds.ForProperty(property),
                resolver.Resolve(property));
        }

        private static ApiFieldModel BuildField(FieldInfo field, Type declaringType, DocumentationResolver resolver)
        {
            string? value = null;
            if (field.IsLiteral)
            {
                var raw = field.GetRawConstantValue();
                value = CSharpTypeNames.Literal(raw);
            }

            return new ApiFieldModel(
                field,
                field.Name,
                CSharpTypeNames.Format(declaringType.IsEnum ? declaringType : field.FieldType),
                field.IsStatic,
                field.IsLiteral,
                field.IsInitOnly,
                value,
                XmlDocIds.ForMember(field)!,
                resolver.Resolve(field));
        }

        private static void LinkDerivedTypes(List<ApiTypeModel> types)
        {
            var byType = types.ToDictionary(model => model.Type);

            foreach (var model in types)
            {
                if (model.BaseType is not null)
                {
                    var baseDefinition = model.BaseType.IsGenericType ? model.BaseType.GetGenericTypeDefinition() : model.BaseType;
                    if (byType.TryGetValue(baseDefinition, out var baseModel))
                    {
                        baseModel.DerivedTypes.Add(model);
                    }
                }

                foreach (var @interface in CSharpTypeNames.DirectInterfaces(model.Type))
                {
                    var definition = @interface.IsGenericType ? @interface.GetGenericTypeDefinition() : @interface;
                    if (byType.TryGetValue(definition, out var interfaceModel))
                    {
                        interfaceModel.DerivedTypes.Add(model);
                    }
                }
            }

            foreach (var model in types)
            {
                model.DerivedTypes.Sort((left, right) => StringComparer.Ordinal.Compare(left.FullName, right.FullName));
            }
        }

        private static void LinkExtensionMethods(List<ApiTypeModel> types)
        {
            var extensionMethods = types.SelectMany(model => model.Methods).Where(method => method.IsExtension).ToList();

            foreach (var model in types)
            {
                if (model.IsStatic || model.Kind is "enum" or "delegate")
                {
                    continue;
                }

                foreach (var method in extensionMethods)
                {
                    if (Accepts(method.ExtendedType!, model.Type))
                    {
                        model.AvailableExtensionMethods.Add(method);
                    }
                }

                model.AvailableExtensionMethods.Sort((left, right) => CompareMethods(left, right));
            }

            foreach (var model in types.Where(model => model.Kind == "enum"))
            {
                foreach (var method in extensionMethods.Where(method => method.ExtendedType == model.Type))
                {
                    model.AvailableExtensionMethods.Add(method);
                }

                model.AvailableExtensionMethods.Sort((left, right) => CompareMethods(left, right));
            }
        }

        private static bool Accepts(Type parameterType, Type target)
        {
            if (parameterType.IsByRef)
            {
                parameterType = parameterType.GetElementType()!;
            }

            if (parameterType == typeof(object))
            {
                return false;
            }

            if (parameterType.IsGenericParameter)
            {
                var constraints = parameterType.GetGenericParameterConstraints();
                return constraints.Length > 0 && constraints.All(constraint => !constraint.ContainsGenericParameters && constraint.IsAssignableFrom(target));
            }

            if (parameterType.ContainsGenericParameters)
            {
                return false;
            }

            return parameterType.IsAssignableFrom(target);
        }

        private static int CompareMethods(ApiMethodModel left, ApiMethodModel right)
        {
            var byName = StringComparer.Ordinal.Compare(left.Name, right.Name);
            if (byName != 0)
            {
                return byName;
            }

            var byCount = left.Parameters.Count.CompareTo(right.Parameters.Count);
            if (byCount != 0)
            {
                return byCount;
            }

            var byId = StringComparer.Ordinal.Compare(left.DocId, right.DocId);
            return byId != 0 ? byId : StringComparer.Ordinal.Compare(left.Signature, right.Signature);
        }

        private static bool IsPublicAccessor(MethodInfo? accessor)
        {
            return accessor is not null && accessor.IsPublic;
        }

        private static string? AccessorAccessibility(MethodInfo? accessor)
        {
            if (accessor is null || accessor.IsPrivate || accessor.IsAssembly || accessor.IsFamilyAndAssembly)
            {
                return null;
            }

            return CSharpTypeNames.Accessibility(accessor);
        }

        private static string AssemblyVersionOf(Assembly assembly)
        {
            return assembly.GetName().Version?.ToString() ?? "0.0.0.0";
        }

        private static Dictionary<string, object?> ArgumentsOf(CustomAttributeData attribute)
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            var parameters = attribute.Constructor.GetParameters();

            for (var index = 0; index < attribute.ConstructorArguments.Count && index < parameters.Length; index++)
            {
                result[parameters[index].Name ?? index.ToString(CultureInfo.InvariantCulture)] = Unwrap(attribute.ConstructorArguments[index]);
            }

            foreach (var named in attribute.NamedArguments)
            {
                // Named property arguments (Name = ..., IsAbstract = ...) override; normalize to camel case.
                result[ReqIfNames.ToFeatureName(named.MemberName)] = Unwrap(named.TypedValue);
            }

            return result;
        }

        private static object? Unwrap(CustomAttributeTypedArgument argument)
        {
            if (argument.ArgumentType.IsEnum && argument.Value is not null)
            {
                return Enum.GetName(argument.ArgumentType, argument.Value) ?? argument.Value.ToString();
            }

            return argument.Value;
        }
    }
}
