// -------------------------------------------------------------------------------------------------
// <copyright file="LocalCliLocator.cs" company="Starion Group S.A.">
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
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Finds a CLI built from source inside the plugin checkout (<c>dotnet build</c> output). Used when no
    /// released CLI could be provisioned, e.g. while developing the plugin or before the first
    /// <c>tools-v*</c> release exists.
    /// </summary>
    public static class LocalCliLocator
    {
        /// <summary>
        /// Looks for <c>tools/sage-cli/ReqIFSharp.Sage.Tools/bin/{Release,Debug}/net10.0/reqifsharp-sage[.exe]</c>.
        /// </summary>
        /// <param name="pluginRoot">The plugin root.</param>
        /// <param name="executableName">The platform's executable name.</param>
        /// <returns>The path of the most recently built CLI, or null.</returns>
        public static string? Find(string pluginRoot, string executableName)
        {
            var binDirectory = Path.Combine(pluginRoot, "tools", "sage-cli", "ReqIFSharp.Sage.Tools", "bin");

            return new[] { "Release", "Debug" }
                .Select(configuration => Path.Combine(binDirectory, configuration, "net10.0", executableName))
                .Where(File.Exists)
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }
    }
}
