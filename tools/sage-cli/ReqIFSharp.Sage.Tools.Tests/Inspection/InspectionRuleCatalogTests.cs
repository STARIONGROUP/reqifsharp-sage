// -------------------------------------------------------------------------------------------------
// <copyright file="InspectionRuleCatalogTests.cs" company="Starion Group S.A.">
//
//   SPDX-FileCopyrightText: 2026 Starion Group S.A.
//   SPDX-License-Identifier: Apache-2.0
//
// </copyright>
// ------------------------------------------------------------------------------------------------

namespace ReqIFSharp.Sage.Tools.Tests.Inspection
{
    using System.Linq;

    using ReqIFSharp.Sage.Tools.Inspection;

    [TestFixture]
    public class InspectionRuleCatalogTests
    {
        [Test]
        public void Rule_ids_are_unique()
        {
            Assert.That(InspectionRuleCatalog.All.Select(rule => rule.Id), Is.Unique);
        }

        [Test]
        public void Every_rule_has_a_known_severity_a_kebab_case_category_and_a_description()
        {
            Assert.Multiple(() =>
            {
                Assert.That(InspectionRuleCatalog.All.Select(rule => rule.Severity), Has.All.AnyOf("error", "warning", "info"));
                Assert.That(InspectionRuleCatalog.All.Select(rule => rule.Category), Has.All.Match("^[a-z]+(-[a-z0-9]+)*$"));
                Assert.That(InspectionRuleCatalog.All.Select(rule => rule.Description), Has.All.Not.Empty);
            });
        }

        [Test]
        public void ReqIF_rules_cite_the_specification_and_guide_rules_cite_the_guide()
        {
            var specRules = InspectionRuleCatalog.All.Where(rule => rule.Id.StartsWith("R-")).ToList();
            var guideRules = InspectionRuleCatalog.All.Where(rule => rule.Id.StartsWith("G-") && rule.Id != "G-000").ToList();

            Assert.Multiple(() =>
            {
                Assert.That(specRules.Select(rule => rule.Reference), Has.All.StartWith("ReqIF 1.2 §"));
                Assert.That(specRules.Select(rule => rule.IsGuideRule), Has.All.False);
                Assert.That(guideRules.Select(rule => rule.Reference), Has.All.StartWith("PSI Implementation Guide 1.7 §"));
                Assert.That(InspectionRuleCatalog.All.Where(rule => rule.Id.StartsWith("G-")).Select(rule => rule.IsGuideRule), Has.All.True);
                Assert.That(InspectionRuleCatalog.All.Where(rule => rule.Id.StartsWith("G-")).Select(rule => rule.Category), Has.All.EqualTo("implementation-guide"));
            });
        }

        [Test]
        public void System_attribute_conventions_are_found_case_insensitively_but_reported_case_correct()
        {
            Assert.Multiple(() =>
            {
                Assert.That(SystemAttributeConventions.FindName("reqif.chaptername"), Is.EqualTo("ReqIF.ChapterName"));
                Assert.That(SystemAttributeConventions.FindName("Custom"), Is.Null);
                Assert.That(SystemAttributeConventions.Find("ReqIF.Text", SystemAttributeConventions.SpecObject)?.DefinitionKind, Is.EqualTo("XHTML"));
                Assert.That(SystemAttributeConventions.Find("ReqIF.ChapterName", SystemAttributeConventions.Specification), Is.Null);
            });
        }
    }
}
