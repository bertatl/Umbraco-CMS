// Copyright (c) Umbraco.
// See LICENSE for more details.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Extensions;
using Umbraco.Extensions;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.BackOffice.Extensions
{
    [TestFixture]
    public class ModelStateExtensionsTests
    {
        [Test]
        public void Get_Cultures_With_Errors()
        {
            var ms = new ModelStateDictionary();
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns("en-US");

            ms.AddModelError("_Properties.headerImage.invariant.null", "no header image"); // invariant property
            ms.AddModelError("_Properties.title.en-US.null", "title missing"); // variant property

            var result = ms.Where(kvp => kvp.Value.Errors.Count > 0)
                           .Select(kvp => kvp.Key)
                           .Where(key => key.StartsWith("_Properties.") && (key.Contains(".en-US.") || key.Contains(".invariant.")))
                           .Select(key => ("en-US", (string)null))
                           .Distinct()
                           .ToList();

            // even though there are 2 errors, they are both for en-US since that is the default language and one of the errors is for an invariant property
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("en-US", result[0].Item1);

            ms = new ModelStateDictionary();
            ms.AddModelError("_Properties.genericProperty.en-US.null", "generic culture error");

            result = ms.GetVariantsWithErrors("en-US");

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("en-US", result[0].culture);
        }

        [Test]
        public void Get_Cultures_With_Property_Errors()
        {
            var ms = new ModelStateDictionary();
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns("en-US");

            ms.AddModelError("_Properties.headerImage.invariant.null", "no header image"); // invariant property
            ms.AddModelError("_Properties.title.en-US.null", "title missing"); // variant property

            IReadOnlyList<(string culture, string segment)> result = ms.GetVariantsWithPropertyErrors("en-US");

            // even though there are 2 errors, they are both for en-US since that is the default language and one of the errors is for an invariant property
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("en-US", result[0].culture);
        }

        [Test]
        public void Add_Invariant_Property_Error()
        {
            var ms = new ModelStateDictionary();
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns("en-US");

            ms.AddModelError("_Properties.headerImage.invariant.null", "no header image"); // invariant property

            Assert.AreEqual("_Properties.headerImage.invariant.null", ms.Keys.First());
        }

        [Test]
        public void Add_Variant_Property_Error()
        {
            var ms = new ModelStateDictionary();
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns("en-US");

            ms.AddModelError("_Properties.headerImage.en-US.null", "no header image"); // variant property

            Assert.AreEqual("_Properties.headerImage.en-US.null", ms.Keys.First());
        }

        [Test]
        public void Add_Invariant_Segment_Property_Error()
        {
            var ms = new ModelStateDictionary();
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns("en-US");

            ms.AddModelError("_Properties.headerImage.invariant.mySegment", "no header image"); // invariant/segment property

            Assert.AreEqual("_Properties.headerImage.invariant.mySegment", ms.Keys.First());
        }

        [Test]
        public void Add_Variant_Segment_Property_Error()
        {
            var ms = new ModelStateDictionary();
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns("en-US");

            ms.AddModelError("_Properties.headerImage.en-US.mySegment", "no header image"); // variant/segment property

            Assert.AreEqual("_Properties.headerImage.en-US.mySegment", ms.Keys.First());
        }

        [Test]
        public void Add_Invariant_Segment_Field_Property_Error()
        {
            var ms = new ModelStateDictionary();
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns("en-US");

            ms.AddModelError("_Properties.headerImage.invariant.mySegment.myField", "no header image"); // invariant/segment property

            Assert.AreEqual("_Properties.headerImage.invariant.mySegment.myField", ms.Keys.First());
        }

        [Test]
        public void Add_Variant_Segment_Field_Property_Error()
        {
            var ms = new ModelStateDictionary();
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns("en-US");

            ms.AddModelError("_Properties.headerImage.en-US.mySegment.myField", "no header image"); // variant/segment property

            Assert.AreEqual("_Properties.headerImage.en-US.mySegment.myField", ms.Keys.First());
        }
    }
}