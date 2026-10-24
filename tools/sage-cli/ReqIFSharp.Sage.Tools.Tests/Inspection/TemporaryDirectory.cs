// -------------------------------------------------------------------------------------------------
// <copyright file="TemporaryDirectory.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Tests.Inspection
{
    using System.IO;

    /// <summary>
    /// Creates scratch folders for the inspection tests and removes them all once the tests of this namespace have run.
    /// </summary>
    [SetUpFixture]
    public class TemporaryDirectory
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "reqifsharp-sage-tests", "inspection-" + Path.GetRandomFileName());

        internal static string Create()
        {
            var directory = Path.Combine(Root, Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
            return directory;
        }

        [OneTimeTearDown]
        public void RemoveAll()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }
}
