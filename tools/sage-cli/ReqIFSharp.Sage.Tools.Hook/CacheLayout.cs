// -------------------------------------------------------------------------------------------------
// <copyright file="CacheLayout.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Hook
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Computes where the full <c>reqifsharp-sage</c> CLI is cached on disk, outside the plugin's own
    /// git-managed tree so a plugin update/reinstall never disturbs it.
    /// </summary>
    public static class CacheLayout
    {
        /// <summary>
        /// The root cache directory: <c>%LOCALAPPDATA%\reqifsharp-sage</c> on Windows, the XDG/home
        /// equivalent elsewhere.
        /// </summary>
        public static string CacheRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "reqifsharp-sage");

        /// <summary>
        /// The directory a specific (version, runtime identifier, plugin root) combination's CLI is
        /// extracted into. Keyed by an install-key hash of the plugin root path so a maintainer checkout
        /// and a marketplace install of the same version never collide.
        /// </summary>
        public static string CliDirectoryFor(string pluginRoot, string version, string runtimeIdentifier)
        {
            return Path.Combine(CacheRoot, "bin", version, runtimeIdentifier, InstallKey(pluginRoot));
        }

        /// <summary>
        /// A short, stable hash of the plugin root path, used to key the cache per-checkout.
        /// </summary>
        public static string InstallKey(string pluginRoot)
        {
            var bytes = Encoding.UTF8.GetBytes(Path.GetFullPath(pluginRoot).ToLowerInvariant());
            return Convert.ToHexStringLower(SHA256.HashData(bytes))[..12];
        }

        /// <summary>
        /// The marker file written once an extracted CLI has been checksum-verified - its presence means
        /// the cached CLI can be trusted without re-verifying on every session start.
        /// </summary>
        public static string VerifiedMarkerPath(string cliDirectory) => Path.Combine(cliDirectory, ".verified");

        /// <summary>
        /// Gets the lock file a background provisioning run holds while it downloads the CLI into
        /// <paramref name="cliDirectory"/>. It sits next to the directory, not inside it.
        /// </summary>
        /// <param name="cliDirectory">The CLI cache directory.</param>
        /// <returns>The lock file path.</returns>
        public static string ProvisioningLockPath(string cliDirectory) => cliDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + ".provisioning";
    }
}
