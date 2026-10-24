// -------------------------------------------------------------------------------------------------
// <copyright file="Fixture.cs" company="Starion Group S.A.">
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

namespace ReqIFSharp.Sage.MetamodelGen.Tests
{
    using System.IO;

    /// <summary>
    /// Paths of the hand-written toy fixtures (a fictional metamodel in ReqIF's shape, never OMG content).
    /// </summary>
    internal static class Fixture
    {
        /// <summary>
        /// Gets the toy CMOF metamodel path.
        /// </summary>
        public static string Cmof => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "toy.cmof");

        /// <summary>
        /// Gets the toy XML schema path.
        /// </summary>
        public static string Xsd => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "toy.xsd");
    }
}
