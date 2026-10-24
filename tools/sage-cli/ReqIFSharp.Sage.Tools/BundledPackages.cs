// -------------------------------------------------------------------------------------------------
// <copyright file="BundledPackages.cs" company="Starion Group S.A.">
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
