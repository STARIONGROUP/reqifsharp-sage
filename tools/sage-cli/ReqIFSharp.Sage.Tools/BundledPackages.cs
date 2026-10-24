// -------------------------------------------------------------------------------------------------
// <copyright file="BundledPackages.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools
{
    using System;
    using System.Reflection;

    /// <summary>
    /// The versions of the reqifsharp packages this CLI is built against: the API reference it generates
    /// and the deserializer <c>inspect</c> uses.
    /// </summary>
    public static class BundledPackages
    {
        /// <summary>
        /// Gets the ReqIFSharp package version.
        /// </summary>
        public static string ReqIFSharpVersion => VersionOf(typeof(ReqIFSharp.ReqIF).Assembly);

        /// <summary>
        /// Gets the ReqIFSharp.Extensions package version.
        /// </summary>
        public static string ExtensionsVersion => VersionOf(typeof(ReqIFSharp.Extensions.Services.ReqIFLoaderService).Assembly);

        /// <summary>
        /// Gets this CLI's own version.
        /// </summary>
        public static string CliVersion => VersionOf(typeof(BundledPackages).Assembly);

        private static string VersionOf(Assembly assembly)
        {
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(informational))
            {
                var plus = informational.IndexOf('+', StringComparison.Ordinal);
                return plus >= 0 ? informational[..plus] : informational;
            }

            return assembly.GetName().Version?.ToString(3) ?? "unknown";
        }
    }
}
