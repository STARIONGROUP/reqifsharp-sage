// -------------------------------------------------------------------------------------------------
// <copyright file="BackgroundProvisioning.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Hook
{
    using System;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// Downloads the CLI outside the SessionStart hook's time budget: the hook starts a detached copy of
    /// itself with <see cref="ProvisionArgument"/>, which provisions the CLI under a generous timeout while
    /// the session starts. A lock file stops several sessions from downloading at the same time.
    /// </summary>
    public static class BackgroundProvisioning
    {
        /// <summary>
        /// The first argument that switches the hook executable into provisioning mode.
        /// </summary>
        public const string ProvisionArgument = "--provision";

        /// <summary>
        /// How long a provisioning run may take, and how long its lock is honoured.
        /// </summary>
        public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets a value indicating whether a provisioning run currently holds the lock for <paramref name="cliDirectory"/>.
        /// </summary>
        /// <param name="cliDirectory">The CLI cache directory.</param>
        /// <param name="now">The current time.</param>
        /// <returns>Whether a recent lock exists; a lock older than <see cref="Timeout"/> is considered stale.</returns>
        public static bool IsRunning(string cliDirectory, DateTime now)
        {
            var lockPath = CacheLayout.ProvisioningLockPath(cliDirectory);
            return File.Exists(lockPath) && now - File.GetLastWriteTimeUtc(lockPath) < Timeout;
        }

        /// <summary>
        /// Starts the detached provisioning process unless one is already running.
        /// </summary>
        /// <param name="pluginRoot">The plugin root.</param>
        /// <param name="cliVersion">The pinned CLI version.</param>
        /// <param name="runtimeIdentifier">The current runtime identifier.</param>
        /// <returns>Whether provisioning is now running (started here or already in progress).</returns>
        public static bool TryStart(string pluginRoot, string cliVersion, string runtimeIdentifier)
        {
            try
            {
                var cliDirectory = CacheLayout.CliDirectoryFor(pluginRoot, cliVersion, runtimeIdentifier);
                if (IsRunning(cliDirectory, DateTime.UtcNow))
                {
                    return true;
                }

                var self = Environment.ProcessPath;
                if (self is null)
                {
                    return false;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(cliDirectory)!);
                File.WriteAllText(CacheLayout.ProvisioningLockPath(cliDirectory), DateTimeOffset.UtcNow.ToString("o"));

                // The child must not inherit the hook's stdout: Claude Code reads the hook's output until
                // that pipe closes, so an inheriting child would hold the session start up. ShellExecute on
                // Windows does not pass handles on; on Unix .NET points the child's stdio at fresh pipes.
                var startInfo = OperatingSystem.IsWindows()
                    ? new ProcessStartInfo(self) { UseShellExecute = true, WindowStyle = ProcessWindowStyle.Hidden, CreateNoWindow = true }
                    : new ProcessStartInfo(self) { UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };

                startInfo.ArgumentList.Add(ProvisionArgument);
                startInfo.ArgumentList.Add(pluginRoot);
                startInfo.ArgumentList.Add(cliVersion);

                using var process = Process.Start(startInfo);
                return process is not null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Releases the lock held for <paramref name="cliDirectory"/>.
        /// </summary>
        /// <param name="cliDirectory">The CLI cache directory.</param>
        public static void ReleaseLock(string cliDirectory)
        {
            try
            {
                File.Delete(CacheLayout.ProvisioningLockPath(cliDirectory));
            }
            catch
            {
                // A stale lock expires after Timeout anyway.
            }
        }
    }
}
