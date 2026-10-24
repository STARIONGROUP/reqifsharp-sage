// -------------------------------------------------------------------------------------------------
// <copyright file="ConsoleCapture.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Tests.Commands
{
    using System;
    using System.CommandLine;
    using System.IO;

    /// <summary>
    /// Runs a command with standard output captured, for the <c>--json</c> verbs (which write JSON
    /// straight to <see cref="Console.Out"/>).
    /// </summary>
    internal static class ConsoleCapture
    {
        /// <summary>
        /// Invokes <paramref name="command"/> with <paramref name="arguments"/> and returns its exit code and output.
        /// </summary>
        public static (int ExitCode, string Output) Run(Command command, params string[] arguments)
        {
            var original = Console.Out;
            using var writer = new StringWriter();
            Console.SetOut(writer);
            try
            {
                var exitCode = command.Parse(arguments).Invoke();
                return (exitCode, writer.ToString());
            }
            finally
            {
                Console.SetOut(original);
            }
        }
    }
}
