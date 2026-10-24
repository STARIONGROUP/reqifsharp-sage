// -------------------------------------------------------------------------------------------------
// <copyright file="CSharpTypeNames.cs" company="Starion Group S.A.">
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
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Formats reflected types, parameters and members the way they are written in C# source:
    /// keywords for built-in types (<c>string</c>, <c>bool</c>), <c>T?</c> for nullable value types,
    /// angle-bracket generic arguments (<c>Task&lt;IEnumerable&lt;ReqIF&gt;&gt;</c>), simple (unqualified)
    /// names for readability.
    /// </summary>
    public static class CSharpTypeNames
    {
        private static readonly Dictionary<Type, string> Keywords = new()
        {
            [typeof(void)] = "void",
            [typeof(object)] = "object",
            [typeof(string)] = "string",
            [typeof(bool)] = "bool",
            [typeof(byte)] = "byte",
            [typeof(sbyte)] = "sbyte",
            [typeof(char)] = "char",
            [typeof(short)] = "short",
            [typeof(ushort)] = "ushort",
            [typeof(int)] = "int",
            [typeof(uint)] = "uint",
            [typeof(long)] = "long",
            [typeof(ulong)] = "ulong",
            [typeof(float)] = "float",
            [typeof(double)] = "double",
            [typeof(decimal)] = "decimal",
            [typeof(nint)] = "nint",
            [typeof(nuint)] = "nuint",
        };

        /// <summary>
        /// Formats a type, e.g. <c>Task&lt;IEnumerable&lt;ReqIF&gt;&gt;</c>, <c>int?</c>, <c>string[]</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="qualified">Whether to prefix (outermost) type names with their namespace.</param>
        /// <returns>The C# spelling.</returns>
        public static string Format(Type type, bool qualified = false)
        {
            if (type.IsByRef || type.IsPointer)
            {
                return Format(type.GetElementType()!, qualified) + (type.IsPointer ? "*" : string.Empty);
            }

            if (type.IsArray)
            {
                var rank = type.GetArrayRank();
                return Format(type.GetElementType()!, qualified) + "[" + new string(',', rank - 1) + "]";
            }

            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (Keywords.TryGetValue(type, out var keyword))
            {
                return keyword;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return Format(type.GetGenericArguments()[0], qualified) + "?";
            }

            var arguments = type.GetGenericArguments();
            var chain = new List<Type>();
            for (var current = type.IsGenericType && !type.IsGenericTypeDefinition ? type.GetGenericTypeDefinition() : type; current is not null; current = current.IsNested ? current.DeclaringType : null)
            {
                chain.Insert(0, current);
            }

            var builder = new StringBuilder();
            if (qualified && !string.IsNullOrEmpty(chain[0].Namespace))
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
                if (own > 0 && arguments.Length >= total)
                {
                    builder.Append('<');
                    builder.Append(string.Join(", ", arguments.Skip(consumed).Take(own).Select(argument => Format(argument, qualified))));
                    builder.Append('>');
                }

                consumed = total;
            }

            return builder.ToString();
        }

        /// <summary>
        /// Formats a parameter as in a C# declaration: modifier, type, name and default value, e.g.
        /// <c>this SpecObject specObject</c>, <c>out int count</c>, <c>bool validate = false</c>.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The C# spelling.</returns>
        public static string FormatParameter(ParameterInfo parameter)
        {
            var builder = new StringBuilder();
            var modifier = ParameterModifier(parameter);
            if (modifier is not null)
            {
                builder.Append(modifier).Append(' ');
            }

            builder.Append(Format(parameter.ParameterType)).Append(' ').Append(parameter.Name);

            var defaultValue = DefaultValue(parameter);
            if (defaultValue is not null)
            {
                builder.Append(" = ").Append(defaultValue);
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the C# modifier of a parameter: <c>this</c> (first parameter of an extension method),
        /// <c>ref</c>, <c>out</c>, <c>in</c> or <c>params</c>.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The modifier, or <see langword="null"/> when there is none.</returns>
        public static string? ParameterModifier(ParameterInfo parameter)
        {
            if (parameter.Position == 0 && parameter.Member is MethodInfo method && method.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false))
            {
                return "this";
            }

            if (parameter.ParameterType.IsByRef)
            {
                if (parameter.IsOut)
                {
                    return "out";
                }

                return parameter.IsIn ? "in" : "ref";
            }

            if (parameter.IsDefined(typeof(ParamArrayAttribute), false))
            {
                return "params";
            }

            return null;
        }

        /// <summary>
        /// Gets the C# spelling of an optional parameter's default value: <c>null</c>, <c>default</c>,
        /// <c>"text"</c>, <c>false</c>, <c>0</c>, <c>SupportedFileExtensionKind.Reqif</c>.
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>The default value, or <see langword="null"/> when the parameter is not optional.</returns>
        public static string? DefaultValue(ParameterInfo parameter)
        {
            if (!parameter.HasDefaultValue)
            {
                return null;
            }

            var value = parameter.RawDefaultValue;
            var type = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

            if (value is null || value is DBNull || value is Missing)
            {
                return type.IsValueType && Nullable.GetUnderlyingType(type) is null ? "default" : "null";
            }

            var enumType = type.IsEnum ? type : Nullable.GetUnderlyingType(type) is { IsEnum: true } underlying ? underlying : null;
            if (enumType is not null)
            {
                var name = Enum.GetName(enumType, value);
                return name is null ? "(" + Format(enumType) + ")" + Literal(value) : Format(enumType) + "." + name;
            }

            return Literal(value);
        }

        /// <summary>
        /// Formats a constant value as a C# literal.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The literal.</returns>
        public static string Literal(object? value)
        {
            return value switch
            {
                null => "null",
                string text => "\"" + text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\"",
                char character => "'" + character + "'",
                bool flag => flag ? "true" : "false",
                float single => single.ToString("R", CultureInfo.InvariantCulture) + "f",
                double number => number.ToString("R", CultureInfo.InvariantCulture),
                decimal number => number.ToString(CultureInfo.InvariantCulture) + "m",
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString() ?? string.Empty,
            };
        }

        /// <summary>
        /// Formats a method's generic parameter list and name, e.g. <c>Query&lt;T&gt;</c>.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The name with type parameters.</returns>
        public static string MethodName(MethodBase method)
        {
            if (method is ConstructorInfo)
            {
                var typeName = method.DeclaringType!.Name;
                var tick = typeName.IndexOf('`', StringComparison.Ordinal);
                return tick < 0 ? typeName : typeName[..tick];
            }

            if (method is MethodInfo { IsGenericMethod: true } generic)
            {
                return method.Name + "<" + string.Join(", ", generic.GetGenericArguments().Select(argument => argument.Name)) + ">";
            }

            return method.Name;
        }

        /// <summary>
        /// Formats a short, overload-distinguishing method heading: name and parameter types, e.g.
        /// <c>Deserialize(string, bool, ValidationEventHandler)</c>.
        /// </summary>
        /// <param name="method">The method or constructor.</param>
        /// <returns>The heading text.</returns>
        public static string MethodHeading(MethodBase method)
        {
            return MethodName(method) + "(" + string.Join(", ", method.GetParameters().Select(parameter =>
            {
                var modifier = ParameterModifier(parameter);
                return (modifier is null ? string.Empty : modifier + " ") + Format(parameter.ParameterType);
            })) + ")";
        }

        /// <summary>
        /// Formats a full C# method or constructor declaration, e.g.
        /// <c>public IEnumerable&lt;ReqIF&gt; Deserialize(string fileUri, bool validate = false, ValidationEventHandler validationEventHandler = null)</c>.
        /// </summary>
        /// <param name="method">The method or constructor.</param>
        /// <returns>The declaration.</returns>
        public static string MethodSignature(MethodBase method)
        {
            var builder = new StringBuilder();
            builder.Append(Accessibility(method)).Append(' ');

            var declaringType = method.DeclaringType;
            var inInterface = declaringType is { IsInterface: true };

            if (method.IsStatic)
            {
                builder.Append("static ");
            }
            else if (!inInterface && method is MethodInfo instanceMethod)
            {
                if (method.IsAbstract)
                {
                    builder.Append("abstract ");
                }
                else if (instanceMethod.GetBaseDefinition() != instanceMethod)
                {
                    builder.Append(method.IsFinal ? "sealed override " : "override ");
                }
                else if (method.IsVirtual && !method.IsFinal)
                {
                    builder.Append("virtual ");
                }
            }

            if (method is MethodInfo info)
            {
                if (info.Name is "op_Implicit" or "op_Explicit")
                {
                    builder.Append(info.Name == "op_Implicit" ? "implicit operator " : "explicit operator ").Append(Format(info.ReturnType));
                }
                else
                {
                    builder.Append(Format(info.ReturnType)).Append(' ').Append(MethodName(method));
                }
            }
            else
            {
                builder.Append(MethodName(method));
            }

            builder.Append('(').Append(string.Join(", ", method.GetParameters().Select(FormatParameter))).Append(')');

            if (method is MethodInfo { IsGenericMethodDefinition: true } genericMethod)
            {
                foreach (var argument in genericMethod.GetGenericArguments())
                {
                    var constraint = Constraints(argument);
                    if (constraint is not null)
                    {
                        builder.Append(" where ").Append(argument.Name).Append(" : ").Append(constraint);
                    }
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the C# accessibility keyword(s) of a method.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns><c>public</c>, <c>protected</c>, <c>protected internal</c>, <c>internal</c> or <c>private</c>.</returns>
        public static string Accessibility(MethodBase method)
        {
            if (method.IsPublic)
            {
                return "public";
            }

            if (method.IsFamilyOrAssembly)
            {
                return "protected internal";
            }

            if (method.IsFamily)
            {
                return "protected";
            }

            return method.IsAssembly ? "internal" : "private";
        }

        /// <summary>
        /// Formats a C# type declaration line, e.g. <c>public abstract class SpecType : Identifiable</c>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="kind">The kind (<c>class</c>, <c>interface</c>, <c>enum</c>, <c>struct</c>, <c>delegate</c>).</param>
        /// <returns>The declaration.</returns>
        public static string TypeDeclaration(Type type, string kind)
        {
            var builder = new StringBuilder("public ");

            if (kind == "delegate")
            {
                var invoke = type.GetMethod("Invoke")!;
                return builder.Append("delegate ").Append(Format(invoke.ReturnType)).Append(' ').Append(Format(type))
                    .Append('(').Append(string.Join(", ", invoke.GetParameters().Select(FormatParameter))).Append(')').ToString();
            }

            if (kind == "class")
            {
                if (type.IsAbstract && type.IsSealed)
                {
                    builder.Append("static ");
                }
                else if (type.IsAbstract)
                {
                    builder.Append("abstract ");
                }
                else if (type.IsSealed)
                {
                    builder.Append("sealed ");
                }
            }

            builder.Append(kind).Append(' ').Append(Format(type));

            var bases = new List<string>();
            if (kind == "enum")
            {
                var underlying = Enum.GetUnderlyingType(type);
                if (underlying != typeof(int))
                {
                    bases.Add(Format(underlying));
                }
            }
            else
            {
                if (kind == "class" && type.BaseType is not null && type.BaseType != typeof(object))
                {
                    bases.Add(Format(type.BaseType));
                }

                bases.AddRange(DirectInterfaces(type).Select(i => Format(i)));
            }

            if (bases.Count > 0)
            {
                builder.Append(" : ").Append(string.Join(", ", bases));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Gets the interfaces a type declares itself (not those it only inherits through its base type or
        /// other interfaces), in ordinal order of their names.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The interfaces.</returns>
        public static IReadOnlyList<Type> DirectInterfaces(Type type)
        {
            var all = type.GetInterfaces();
            var inherited = new HashSet<Type>();

            if (type.BaseType is not null)
            {
                inherited.UnionWith(type.BaseType.GetInterfaces());
            }

            foreach (var @interface in all)
            {
                inherited.UnionWith(@interface.GetInterfaces());
            }

            return all.Where(i => !inherited.Contains(i))
                .OrderBy(i => Format(i, qualified: true), StringComparer.Ordinal)
                .ToList();
        }

        private static string? Constraints(Type genericParameter)
        {
            var parts = new List<string>();
            var attributes = genericParameter.GenericParameterAttributes;

            if ((attributes & GenericParameterAttributes.ReferenceTypeConstraint) != 0)
            {
                parts.Add("class");
            }

            if ((attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
            {
                parts.Add("struct");
            }

            parts.AddRange(genericParameter.GetGenericParameterConstraints()
                .Where(constraint => constraint != typeof(ValueType))
                .Select(constraint => Format(constraint))
                .OrderBy(name => name, StringComparer.Ordinal));

            if ((attributes & GenericParameterAttributes.DefaultConstructorConstraint) != 0
                && (attributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == 0)
            {
                parts.Add("new()");
            }

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }
    }
}
