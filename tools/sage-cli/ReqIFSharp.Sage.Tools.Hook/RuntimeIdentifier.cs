// -------------------------------------------------------------------------------------------------
// <copyright file="RuntimeIdentifier.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Hook
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Resolves the current platform's .NET runtime identifier (one of the four the release workflow
    /// publishes: <c>win-x64</c>, <c>linux-x64</c>, <c>osx-x64</c>, <c>osx-arm64</c>).
    /// </summary>
    public static class RuntimeIdentifier
    {
        /// <summary>
        /// The current platform's runtime identifier, or <c>null</c> if it isn't one of the four published.
        /// </summary>
        public static string? Current()
        {
            var architecture = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => null,
            };

            if (architecture is null)
            {
                return null;
            }

            if (OperatingSystem.IsWindows() && architecture == "x64")
            {
                return "win-x64";
            }

            if (OperatingSystem.IsLinux() && architecture == "x64")
            {
                return "linux-x64";
            }

            if (OperatingSystem.IsMacOS())
            {
                return architecture == "arm64" ? "osx-arm64" : "osx-x64";
            }

            return null;
        }

        /// <summary>
        /// The executable file name for a given runtime identifier (adds ".exe" on Windows).
        /// </summary>
        public static string ExecutableName(string runtimeIdentifier) => runtimeIdentifier.StartsWith("win-", StringComparison.Ordinal) ? "reqifsharp-sage.exe" : "reqifsharp-sage";
    }
}
