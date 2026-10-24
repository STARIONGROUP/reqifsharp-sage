// -------------------------------------------------------------------------------------------------
// <copyright file="XmlDocIds.cs" company="Starion Group S.A.">
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
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Computes the C# compiler's XML documentation member ids (<c>T:</c>, <c>M:</c>, <c>P:</c>, <c>F:</c>,
    /// <c>E:</c>) for reflected types and members, so they can be looked up in the generated
    /// <c>.xml</c> documentation file. Follows the ECMA-334 "processing the documentation file" rules:
    /// nested types are joined with <c>.</c>, generic type definitions keep their <c>`n</c> arity,
    /// constructed generic arguments are written in braces, type generic parameters are <c>`n</c>,
    /// method generic parameters are <c>``n</c>, by-ref parameters end with <c>@</c>.
    /// </summary>
    public static class XmlDocIds
    {
        /// <summary>
        /// Gets the documentation id of a type, e.g. <c>T:ReqIFSharp.SpecObject</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The id.</returns>
        public static string ForType(Type type)
        {
            return "T:" + TypeDefinitionName(type);
        }

        /// <summary>
        /// Gets the documentation id of a member (method, constructor, property, field or event).
        /// </summary>
        /// <param name="member">The member.</param>
        /// <returns>The id, or <see langword="null"/> for member kinds that have no id (e.g. nested types,
        /// whose id is given by <see cref="ForType"/>).</returns>
        public static string? ForMember(MemberInfo member)
        {
            switch (member)
            {
                case Type type:
                    return ForType(type);
                case MethodBase method:
                    return ForMethod(method);
                case PropertyInfo property:
                    return ForProperty(property);
                case FieldInfo field:
                    return "F:" + TypeDefinitionName(field.DeclaringType!) + "." + MemberName(field.Name);
                case EventInfo @event:
                    return "E:" + TypeDefinitionName(@event.DeclaringType!) + "." + MemberName(@event.Name);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Gets the documentation id of a method or constructor, e.g.
        /// <c>M:ReqIFSharp.ReqIFDeserializer.Deserialize(System.String,System.Boolean,System.Xml.Schema.ValidationEventHandler)</c>.
        /// </summary>
        /// <param name="method">The method or constructor.</param>
        /// <returns>The id.</returns>
        public static string ForMethod(MethodBase method)
        {
            var builder = new StringBuilder("M:");
            builder.Append(TypeDefinitionName(method.DeclaringType!)).Append('.');

            builder.Append(method switch
            {
                ConstructorInfo { IsStatic: true } => "#cctor",
                ConstructorInfo => "#ctor",
                _ => MemberName(method.Name),
            });

            if (method is MethodInfo { IsGenericMethod: true } genericMethod)
            {
                builder.Append("``").Append(genericMethod.GetGenericArguments().Length);
            }

            var parameters = method.GetParameters();
            if (parameters.Length > 0)
            {
                builder.Append('(');
                builder.Append(string.Join(",", parameters.Select(parameter => ParameterTypeName(parameter.ParameterType))));
                builder.Append(')');
            }

            if (method is MethodInfo conversion && method.Name is "op_Implicit" or "op_Explicit")
            {
                builder.Append('~').Append(ParameterTypeName(conversion.ReturnType));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the documentation id of a property, e.g. <c>P:ReqIFSharp.Identifiable.LongName</c>; indexers
        /// carry their parameter list.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <returns>The id.</returns>
        public static string ForProperty(PropertyInfo property)
        {
            var builder = new StringBuilder("P:");
            builder.Append(TypeDefinitionName(property.DeclaringType!)).Append('.').Append(MemberName(property.Name));

            var parameters = property.GetIndexParameters();
            if (parameters.Length > 0)
            {
                builder.Append('(');
                builder.Append(string.Join(",", parameters.Select(parameter => ParameterTypeName(parameter.ParameterType))));
                builder.Append(')');
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the documentation-id spelling of a type used in a parameter list: generic arguments in braces,
        /// generic parameters as <c>`n</c> / <c>``n</c>, arrays as <c>[]</c>, by-ref as <c>@</c>.
        /// </summary>
        /// <param name="type">The parameter type.</param>
        /// <returns>The id spelling.</returns>
        public static string ParameterTypeName(Type type)
        {
            if (type.IsByRef)
            {
                return ParameterTypeName(type.GetElementType()!) + "@";
            }

            if (type.IsPointer)
            {
                return ParameterTypeName(type.GetElementType()!) + "*";
            }

            if (type.IsArray)
            {
                var rank = type.GetArrayRank();
                var suffix = rank == 1 && type.IsSZArray ? "[]" : "[" + string.Join(",", Enumerable.Repeat("0:", rank)) + "]";
                return ParameterTypeName(type.GetElementType()!) + suffix;
            }

            if (type.IsGenericParameter)
            {
                return type.DeclaringMethod is null ? "`" + type.GenericParameterPosition : "``" + type.GenericParameterPosition;
            }

            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                return ConstructedName(type);
            }

            return TypeDefinitionName(type);
        }

        /// <summary>
        /// Gets the documentation-id name of a type definition: namespace, then nested-type chain joined with
        /// <c>.</c>, each keeping its <c>`n</c> arity suffix.
        /// </summary>
        /// <param name="type">The type (a constructed generic type is reduced to its definition).</param>
        /// <returns>The name, e.g. <c>System.Collections.Generic.List`1</c>.</returns>
        public static string TypeDefinitionName(Type type)
        {
            if (type.IsGenericType && !type.IsGenericTypeDefinition)
            {
                type = type.GetGenericTypeDefinition();
            }

            if (type.IsNested)
            {
                return TypeDefinitionName(type.DeclaringType!) + "." + type.Name;
            }

            return string.IsNullOrEmpty(type.Namespace) ? type.Name : type.Namespace + "." + type.Name;
        }

        private static string ConstructedName(Type type)
        {
            var arguments = type.GetGenericArguments();
            var definition = type.GetGenericTypeDefinition();

            // Walk the nesting chain outermost-first, handing each level the generic arguments it introduced.
            var chain = new System.Collections.Generic.List<Type>();
            for (var current = definition; current is not null; current = current.IsNested ? current.DeclaringType : null)
            {
                chain.Insert(0, current);
            }

            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(chain[0].Namespace))
            {
                builder.Append(chain[0].Namespace).Append('.');
            }

            var consumed = 0;
            for (var level = 0; level < chain.Count; level++)
            {
                if (level > 0)
                {
                    builder.Append('.');
                }

                var name = chain[level].Name;
                var tick = name.IndexOf('`', StringComparison.Ordinal);
                builder.Append(tick < 0 ? name : name[..tick]);

                var total = chain[level].GetGenericArguments().Length;
                var own = total - consumed;
                if (own > 0)
                {
                    builder.Append('{');
                    builder.Append(string.Join(",", arguments.Skip(consumed).Take(own).Select(ParameterTypeName)));
                    builder.Append('}');
                }

                consumed = total;
            }

            return builder.ToString();
        }

        private static string MemberName(string name)
        {
            // Explicit interface implementations are named "Ns.IFoo.Bar"; ids replace the dots with '#'.
            return name.Replace('.', '#');
        }
    }
}
