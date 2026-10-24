// -------------------------------------------------------------------------------------------------
// <copyright file="RepositoryRootResolver.cs" company="Starion Group S.A.">
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
    using System.IO;

    /// <summary>
    /// Works out the plugin repository root a command operates on.
    /// </summary>
    public static class RepositoryRootResolver
    {
        /// <summary>
        /// Resolves the root, in order: <paramref name="explicitValue"/> (<c>--repository-root</c>),
        /// <c>$CLAUDE_PLUGIN_ROOT</c>, the nearest ancestor of the current directory that contains
        /// <c>.claude-plugin/plugin.json</c>, and finally the current directory.
        /// </summary>
        /// <param name="explicitValue">The <c>--repository-root</c> value, if given.</param>
        /// <returns>The absolute root path.</returns>
        public static string Resolve(string? explicitValue)
        {
            if (!string.IsNullOrWhiteSpace(explicitValue))
            {
                return Path.GetFullPath(explicitValue);
            }

            var pluginRoot = Environment.GetEnvironmentVariable("CLAUDE_PLUGIN_ROOT");
            if (!string.IsNullOrWhiteSpace(pluginRoot))
            {
                return Path.GetFullPath(pluginRoot);
            }

            var current = Directory.GetCurrentDirectory();
            for (var directory = new DirectoryInfo(current); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, ".claude-plugin", "plugin.json")))
                {
                    return directory.FullName;
                }
            }

            return current;
        }
    }
}
