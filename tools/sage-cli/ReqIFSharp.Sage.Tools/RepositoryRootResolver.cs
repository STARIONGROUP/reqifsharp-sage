// -------------------------------------------------------------------------------------------------
// <copyright file="RepositoryRootResolver.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
