// -------------------------------------------------------------------------------------------------
// <copyright file="DocumentationResolver.cs" company="Starion Group S.A.">
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
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;

    /// <summary>
    /// Resolves the documentation of reflected types and members against an <see cref="XmlDocumentationSet"/>,
    /// including <c>&lt;inheritdoc/&gt;</c>: an explicit <c>cref</c> is followed; otherwise a type inherits
    /// from its base type and then its interfaces, and a member from the member it overrides in a base type
    /// and then from the interface member it implements. An undocumented override or interface implementation
    /// inherits implicitly, as it would with <c>&lt;inheritdoc/&gt;</c>. Parts written on the member itself
    /// win over inherited parts.
    /// </summary>
    public sealed class DocumentationResolver
    {
        private const int MaximumDepth = 16;

        private const BindingFlags AllDeclared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
            | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private readonly XmlDocumentationSet documentation;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentationResolver"/> class.
        /// </summary>
        /// <param name="documentation">The documentation files to resolve against.</param>
        public DocumentationResolver(XmlDocumentationSet documentation)
        {
            this.documentation = documentation;
        }

        /// <summary>
        /// Resolves the documentation of <paramref name="member"/> (a <see cref="Type"/> or any member).
        /// </summary>
        /// <param name="member">The type or member.</param>
        /// <returns>The resolved comment, or <see cref="DocComment.Empty"/> when nothing is documented.</returns>
        public DocComment Resolve(MemberInfo member)
        {
            return this.Resolve(member, 0, new HashSet<string>(StringComparer.Ordinal));
        }

        /// <summary>
        /// Converts a raw <c>&lt;member&gt;</c> element into a <see cref="DocComment"/> without resolving
        /// <c>&lt;inheritdoc/&gt;</c>.
        /// </summary>
        /// <param name="member">The element, or <see langword="null"/>.</param>
        /// <returns>The comment.</returns>
        public static DocComment FromElement(XElement? member)
        {
            if (member is null)
            {
                return DocComment.Empty;
            }

            return new DocComment(
                member.Element("summary"),
                member.Element("remarks"),
                member.Element("returns"),
                member.Element("value"),
                member.Element("example"),
                ByName(member.Elements("param")),
                ByName(member.Elements("typeparam")),
                member.Elements("exception").ToList(),
                null);
        }

        private DocComment Resolve(MemberInfo member, int depth, HashSet<string> visited)
        {
            var id = XmlDocIds.ForMember(member);
            if (id is null || depth > MaximumDepth || !visited.Add(id))
            {
                return DocComment.Empty;
            }

            var element = this.documentation.Find(id);
            var own = FromElement(element);
            var inheritDoc = element?.Element("inheritdoc");

            var shouldInherit = inheritDoc is not null || (element is null && member is not Type);
            if (!shouldInherit)
            {
                return own;
            }

            var cref = (string?)inheritDoc?.Attribute("cref");
            if (!string.IsNullOrEmpty(cref))
            {
                if (!visited.Add(cref))
                {
                    return own;
                }

                var target = this.documentation.Find(cref);
                return target is null ? own : Merge(own, FromElement(target), cref);
            }

            foreach (var candidate in InheritanceCandidates(member))
            {
                var inherited = this.Resolve(candidate, depth + 1, visited);
                if (!inherited.IsEmpty)
                {
                    return Merge(own, inherited, inherited.InheritedFrom ?? XmlDocIds.ForMember(candidate));
                }
            }

            return own;
        }

        /// <summary>
        /// Enumerates, in priority order, the types or members <paramref name="member"/> inherits documentation from.
        /// </summary>
        /// <param name="member">The type or member.</param>
        /// <returns>The candidates.</returns>
        public static IEnumerable<MemberInfo> InheritanceCandidates(MemberInfo member)
        {
            switch (member)
            {
                case Type type:
                    if (type.BaseType is not null && type.BaseType != typeof(object))
                    {
                        yield return type.BaseType;
                    }

                    foreach (var @interface in type.GetInterfaces().OrderBy(i => XmlDocIds.ForType(i), StringComparer.Ordinal))
                    {
                        yield return @interface;
                    }

                    break;

                case MethodInfo method:
                    foreach (var candidate in MethodCandidates(method))
                    {
                        yield return candidate;
                    }

                    break;

                case ConstructorInfo constructor:
                    var baseType = constructor.DeclaringType?.BaseType;
                    var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();
                    var baseConstructor = baseType?.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, parameterTypes, null);
                    if (baseConstructor is not null)
                    {
                        yield return baseConstructor;
                    }

                    break;

                case PropertyInfo property:
                    foreach (var candidate in PropertyCandidates(property))
                    {
                        yield return candidate;
                    }

                    break;

                case EventInfo @event:
                    for (var current = @event.DeclaringType?.BaseType; current is not null; current = current.BaseType)
                    {
                        var baseEvent = current.GetEvent(@event.Name, AllDeclared);
                        if (baseEvent is not null)
                        {
                            yield return baseEvent;
                            break;
                        }
                    }

                    foreach (var @interface in InterfacesOf(@event.DeclaringType))
                    {
                        var interfaceEvent = @interface.GetEvent(@event.Name);
                        if (interfaceEvent is not null)
                        {
                            yield return interfaceEvent;
                        }
                    }

                    break;
            }
        }

        private static IEnumerable<MemberInfo> MethodCandidates(MethodInfo method)
        {
            var declaringType = method.DeclaringType;
            if (declaringType is null)
            {
                yield break;
            }

            if (method.IsVirtual && method.GetBaseDefinition() != method)
            {
                for (var current = declaringType.BaseType; current is not null; current = current.BaseType)
                {
                    var baseMethod = current.GetMethods(AllDeclared).FirstOrDefault(m => m.Name == method.Name && SameParameters(m, method));
                    if (baseMethod is not null)
                    {
                        yield return baseMethod;
                        break;
                    }
                }
            }

            if (declaringType.IsInterface)
            {
                yield break;
            }

            foreach (var @interface in InterfacesOf(declaringType))
            {
                var map = declaringType.GetInterfaceMap(@interface);
                for (var index = 0; index < map.TargetMethods.Length; index++)
                {
                    if (map.TargetMethods[index] == method)
                    {
                        yield return map.InterfaceMethods[index];
                    }
                }
            }
        }

        private static IEnumerable<MemberInfo> PropertyCandidates(PropertyInfo property)
        {
            var declaringType = property.DeclaringType;
            var accessor = property.GetMethod ?? property.SetMethod;
            if (declaringType is null || accessor is null)
            {
                yield break;
            }

            if (accessor.IsVirtual && accessor.GetBaseDefinition() != accessor)
            {
                for (var current = declaringType.BaseType; current is not null; current = current.BaseType)
                {
                    var baseProperty = current.GetProperties(AllDeclared).FirstOrDefault(p => p.Name == property.Name);
                    if (baseProperty is not null)
                    {
                        yield return baseProperty;
                        break;
                    }
                }
            }

            if (declaringType.IsInterface)
            {
                yield break;
            }

            foreach (var @interface in InterfacesOf(declaringType))
            {
                var map = declaringType.GetInterfaceMap(@interface);
                for (var index = 0; index < map.TargetMethods.Length; index++)
                {
                    if (map.TargetMethods[index] != accessor)
                    {
                        continue;
                    }

                    var interfaceAccessor = map.InterfaceMethods[index];
                    var interfaceProperty = @interface.GetProperties().FirstOrDefault(p => p.GetMethod == interfaceAccessor || p.SetMethod == interfaceAccessor);
                    if (interfaceProperty is not null)
                    {
                        yield return interfaceProperty;
                    }
                }
            }
        }

        private static IEnumerable<Type> InterfacesOf(Type? type)
        {
            if (type is null || type.IsInterface)
            {
                return Array.Empty<Type>();
            }

            return type.GetInterfaces().OrderBy(i => XmlDocIds.ForType(i), StringComparer.Ordinal);
        }

        private static bool SameParameters(MethodInfo left, MethodInfo right)
        {
            var leftParameters = left.GetParameters();
            var rightParameters = right.GetParameters();
            if (leftParameters.Length != rightParameters.Length)
            {
                return false;
            }

            for (var index = 0; index < leftParameters.Length; index++)
            {
                if (XmlDocIds.ParameterTypeName(leftParameters[index].ParameterType) != XmlDocIds.ParameterTypeName(rightParameters[index].ParameterType))
                {
                    return false;
                }
            }

            return true;
        }

        private static DocComment Merge(DocComment own, DocComment inherited, string? source)
        {
            var parameters = new Dictionary<string, XElement>(inherited.Parameters, StringComparer.Ordinal);
            foreach (var pair in own.Parameters)
            {
                parameters[pair.Key] = pair.Value;
            }

            var typeParameters = new Dictionary<string, XElement>(inherited.TypeParameters, StringComparer.Ordinal);
            foreach (var pair in own.TypeParameters)
            {
                typeParameters[pair.Key] = pair.Value;
            }

            return new DocComment(
                own.Summary ?? inherited.Summary,
                own.Remarks ?? inherited.Remarks,
                own.Returns ?? inherited.Returns,
                own.Value ?? inherited.Value,
                own.Example ?? inherited.Example,
                parameters,
                typeParameters,
                own.Exceptions.Count > 0 ? own.Exceptions : inherited.Exceptions,
                source);
        }

        private static IReadOnlyDictionary<string, XElement> ByName(IEnumerable<XElement> elements)
        {
            var result = new Dictionary<string, XElement>(StringComparer.Ordinal);
            foreach (var element in elements)
            {
                var name = (string?)element.Attribute("name");
                if (!string.IsNullOrEmpty(name))
                {
                    result.TryAdd(name, element);
                }
            }

            return result;
        }
    }
}
