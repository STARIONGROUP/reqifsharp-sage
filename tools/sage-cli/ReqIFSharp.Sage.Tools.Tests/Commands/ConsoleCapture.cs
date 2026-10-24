// -------------------------------------------------------------------------------------------------
// <copyright file="ConsoleCapture.cs" company="Starion Group S.A.">
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
