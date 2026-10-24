// -------------------------------------------------------------------------------------------------
// <copyright file="Fixture.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
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
