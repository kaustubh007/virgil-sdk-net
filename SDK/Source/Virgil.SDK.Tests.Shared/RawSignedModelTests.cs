﻿using System;
using System.Collections.Generic;
using System.Text;
using Bogus;
using FluentAssertions;
using NUnit.Framework;
using Virgil.Crypto;
using Virgil.SDK.Common;
using Virgil.SDK.Signer;
using Virgil.SDK.Web;

namespace Virgil.SDK.Tests.Shared
{
    [TestFixture]
    class RawSignedModelTests
    {
        private readonly Faker faker = new Faker();

        [Test]
        public void GeneratePureModelFromString_Should_ReturnEquivalentModel()
        {
            //STC-1
            var model = faker.PredefinedRawSignedModel();
            var exportedStr = model.ExportAsString();

            var importedModel = RawSignedModelUtils.GenerateFromString(exportedStr);
            importedModel.ShouldBeEquivalentTo(model);
        }

        [Test]
        public void Export_WithEmptyPreviousCardId_ShouldNot_HavePreviousCardIdInJson()
        {
            var model = faker.PredefinedRawSignedModel();
            var exportedStr = model.ExportAsJson();

            Assert.IsFalse(exportedStr.Contains("previous_card_id"));
        }


        [Test]
        public void GeneratePureModelFromJson_Should_ReturnEquivalentModel()
        {
            //STC-1
            var model = faker.PredefinedRawSignedModel();
            var exportedJson = model.ExportAsJson();

            var importedModel = RawSignedModelUtils.GenerateFromJson(exportedJson);
            Assert.AreEqual(importedModel, model);
        }

        [Test]
        public void GenerateFullModelFromString_Should_ReturnEquivalentModel()
        {
            //STC-2
            var model = faker.PredefinedRawSignedModel(
                "a666318071274adb738af3f67b8c7ec29d954de2cabfd71a942e6ea38e59fff9",
                true, true, true);
 
            var exportedStr = model.ExportAsString();
            var importedModel = RawSignedModelUtils.GenerateFromString(exportedStr);
            Assert.AreEqual(importedModel, model);
        }

        [Test]
        public void GenerateFullModelFromJson_Should_ReturnEquivalentModel()
        {
            //STC-2
            var model = faker.PredefinedRawSignedModel(
                "a666318071274adb738af3f67b8c7ec29d954de2cabfd71a942e6ea38e59fff9",
                true, true, true);
            var exportedJson = model.ExportAsJson();
            var importedModel = RawSignedModelUtils.GenerateFromJson(exportedJson);
            Assert.AreEqual(importedModel, model);
        }

    }
}