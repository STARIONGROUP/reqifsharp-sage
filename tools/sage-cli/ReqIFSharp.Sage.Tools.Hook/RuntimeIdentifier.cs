// -------------------------------------------------------------------------------------------------
// <copyright file="RuntimeIdentifier.cs" company="Starion Group S.A.">
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
