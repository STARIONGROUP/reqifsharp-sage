// -------------------------------------------------------------------------------------------------
// <copyright file="TemporaryDirectory.cs" company="Starion Group S.A.">
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
