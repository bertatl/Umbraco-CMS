﻿using Microsoft.Extensions.Caching.Hybrid;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.PublishedCache;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Cms.Tests.Integration.Umbraco.Infrastructure.Services;

[TestFixture]
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
public class ContentHybridCacheTests : UmbracoIntegrationTestWithContentEditing
{
    protected override void CustomTestSetup(IUmbracoBuilder builder) => builder.AddUmbracoHybridCache();

    private IPublishedContentCache PublishedContentHybridCache => GetRequiredService<IPublishedContentCache>();

    private IContentEditingService ContentEditingService => GetRequiredService<IContentEditingService>();

    private IContentPublishingService ContentPublishingService => GetRequiredService<IContentPublishingService>();

    private IUmbracoContextFactory UmbracoContextFactory => GetRequiredService<IUmbracoContextFactory>();

    private const string NewName = "New Name";
    private const string NewTitle = "New Title";


    // Missing
    // Cultures, Scope Rollback, More property tests with element cache level & element
    // Create CRUD Tests for Content, Also cultures.

    [Test]
    public async Task Can_Get_Draft_Content_By_Id()
    {
        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(TextpageId, true);

        // Assert
        AssertTextPage(textPage);
    }

    [Test]
    public async Task Can_Get_Draft_Content_By_Key()
    {
        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(Textpage.Key.Value, true);

        // Assert
        AssertTextPage(textPage);
    }

    [Test]
    public async Task Can_Get_Published_Content_By_Id()
    {
        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPageId);

        // Assert
        AssertPublishedTextPage(textPage);
    }

    [Test]
    public async Task Can_Get_Published_Content_By_Key()
    {
        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value);

        // Assert
        AssertPublishedTextPage(textPage);
    }

    [Test]
    public async Task Has_Content_By_Id_Returns_False_If_Not_In_Cache()
    {
        // Act
        var hasContent = await PublishedContentHybridCache.HasByIdAsync(TextpageId);

        // Assert
        Assert.IsFalse(hasContent);
    }

    [Test]
    public async Task Has_Content_By_Id_Returns_True_If_In_Cache()
    {
        // Act
        var hasContent = await PublishedContentHybridCache.HasByIdAsync(TextpageId, true);

        // Assert
        Assert.IsTrue(hasContent);
    }

    [Test]
    public async Task Has_Content_By_Id_Has_Content_After_Load()
    {
        // Arrange
        await ContentPublishingService.PublishAsync(Textpage.Key.Value, CultureAndSchedule, Constants.Security.SuperUserKey);
        await PublishedContentHybridCache.GetByIdAsync(TextpageId);
        var hybridCache = GetRequiredService<HybridCache>();
        await hybridCache.RemoveAsync(Textpage.Key.Value.ToString());
        var hasContent = await PublishedContentHybridCache.HasByIdAsync(TextpageId, true);
        Assert.IsFalse(hasContent);
        await PublishedContentHybridCache.GetByIdAsync(TextpageId, true);

        // Act
        var hasContent2 = await PublishedContentHybridCache.HasByIdAsync(TextpageId, true);

        // Assert
        Assert.IsTrue(hasContent2);
    }

    [Test]
    public async Task Can_Get_Draft_Of_Published_Content_By_Id()
    {
        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPageId, true);

        // Assert
        AssertPublishedTextPage(textPage);
        Assert.IsFalse(textPage.IsPublished());
    }

    [Test]
    public async Task Can_Get_Draft_Of_Published_Content_By_Key()
    {
        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value, true);

        // Assert
        AssertPublishedTextPage(textPage);
        Assert.IsFalse(textPage.IsPublished());
    }

    [Test]
    public async Task Can_Get_Updated_Draft_Content_By_Id()
    {
        // Arrange
        Textpage.InvariantName = NewName;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = NewName,
            InvariantProperties = Textpage.InvariantProperties,
            Variants = Textpage.Variants,
            TemplateKey = Textpage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(Textpage.Key.Value, updateModel, Constants.Security.SuperUserKey);

        // Act
        var updatedPage = await PublishedContentHybridCache.GetByIdAsync(TextpageId, true);

        // Assert
        Assert.AreEqual(NewName, updatedPage.Name);
    }

    [Test]
    public async Task Can_Get_Updated_Draft_Content_By_Key()
    {
        // Arrange
        Textpage.InvariantName = NewName;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = NewName,
            InvariantProperties = Textpage.InvariantProperties,
            Variants = Textpage.Variants,
            TemplateKey = Textpage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(Textpage.Key.Value, updateModel, Constants.Security.SuperUserKey);

        // Act
        var updatedPage = await PublishedContentHybridCache.GetByIdAsync(Textpage.Key.Value, true);

        // Assert
        Assert.AreEqual(NewName, updatedPage.Name);
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(false, false)]
    // BETTER NAMING, CURRENTLY THIS IS TESTING BOTH THE PUBLISHED AND THE DRAFT OF THE PUBLISHED.
    public async Task Can_Get_Updated_Draft_Published_Content_By_Id(bool preview, bool result)
    {
        // Arrange
        PublishedTextPage.InvariantName = NewName;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = NewName,
            InvariantProperties = PublishedTextPage.InvariantProperties,
            Variants = PublishedTextPage.Variants,
            TemplateKey = PublishedTextPage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(PublishedTextPage.Key.Value, updateModel, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPageId, preview);

        // Assert
        Assert.AreEqual(result, NewName.Equals(textPage.Name));
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(false, false)]
    // BETTER NAMING, CURRENTLY THIS IS TESTING BOTH THE PUBLISHED AND THE DRAFT OF THE PUBLISHED.
    public async Task Can_Get_Updated_Draft_Published_Content_By_Key(bool preview, bool result)
    {
        // Arrange
        PublishedTextPage.InvariantName = NewName;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = NewName,
            InvariantProperties = PublishedTextPage.InvariantProperties,
            Variants = PublishedTextPage.Variants,
            TemplateKey = PublishedTextPage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(PublishedTextPage.Key.Value, updateModel, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value, preview);

        // Assert
        Assert.AreEqual(result, NewName.Equals(textPage.Name));
    }

    [Test]
    public async Task Can_Get_Draft_Content_Property_By_Id()
    {
        // Arrange
        var titleValue = Textpage.InvariantProperties.First(x => x.Alias == "title").Value;

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(TextpageId, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(titleValue, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Draft_Content_Property_By_Key()
    {
        // Arrange
        var titleValue = Textpage.InvariantProperties.First(x => x.Alias == "title").Value;

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(Textpage.Key.Value, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(titleValue, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Published_Content_Property_By_Id()
    {
        // Arrange
        var titleValue = PublishedTextPage.InvariantProperties.First(x => x.Alias == "title").Value;

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPageId, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(titleValue, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Published_Content_Property_By_Key()
    {
        // Arrange
        var titleValue = PublishedTextPage.InvariantProperties.First(x => x.Alias == "title").Value;

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(titleValue, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Draft_Of_Published_Content_Property_By_Id()
    {
        // Arrange
        var titleValue = PublishedTextPage.InvariantProperties.First(x => x.Alias == "title").Value;

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPageId, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(titleValue, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Draft_Of_Published_Content_Property_By_Key()
    {
        // Arrange
        var titleValue = PublishedTextPage.InvariantProperties.First(x => x.Alias == "title").Value;

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(titleValue, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Updated_Draft_Content_Property_By_Id()
    {
        // Arrange
        Textpage.InvariantProperties.First(x => x.Alias == "title").Value = NewTitle;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = Textpage.InvariantName,
            InvariantProperties = Textpage.InvariantProperties,
            Variants = Textpage.Variants,
            TemplateKey = Textpage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(Textpage.Key.Value, updateModel, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(TextpageId, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(NewTitle, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Updated_Draft_Content_Property_By_Key()
    {
        // Arrange
        Textpage.InvariantProperties.First(x => x.Alias == "title").Value = NewTitle;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = Textpage.InvariantName,
            InvariantProperties = Textpage.InvariantProperties,
            Variants = Textpage.Variants,
            TemplateKey = Textpage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(Textpage.Key.Value, updateModel, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(Textpage.Key.Value, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(NewTitle, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Updated_Published_Content_Property_By_Id()
    {
        // Arrange
        PublishedTextPage.InvariantProperties.First(x => x.Alias == "title").Value = NewTitle;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = PublishedTextPage.InvariantName,
            InvariantProperties = PublishedTextPage.InvariantProperties,
            Variants = PublishedTextPage.Variants,
            TemplateKey = PublishedTextPage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(PublishedTextPage.Key.Value, updateModel, Constants.Security.SuperUserKey);
        await ContentPublishingService.PublishAsync(PublishedTextPage.Key.Value, CultureAndSchedule, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(NewTitle, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Get_Updated_Published_Content_Property_By_Key()
    {
        // Arrange
        PublishedTextPage.InvariantProperties.First(x => x.Alias == "title").Value = NewTitle;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = PublishedTextPage.InvariantName,
            InvariantProperties = PublishedTextPage.InvariantProperties,
            Variants = PublishedTextPage.Variants,
            TemplateKey = PublishedTextPage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(PublishedTextPage.Key.Value, updateModel, Constants.Security.SuperUserKey);
        await ContentPublishingService.PublishAsync(PublishedTextPage.Key.Value, CultureAndSchedule, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(NewTitle, textPage.Value("title"));
    }

    [Test]
    [TestCase(true, "New Title")]
    [TestCase(false, "Welcome to our Home page")]
    public async Task Can_Get_Updated_Draft_Of_Published_Content_Property_By_Id(bool preview, string titleName)
    {
        // Arrange
        PublishedTextPage.InvariantProperties.First(x => x.Alias == "title").Value = NewTitle;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = PublishedTextPage.InvariantName,
            InvariantProperties = PublishedTextPage.InvariantProperties,
            Variants = PublishedTextPage.Variants,
            TemplateKey = PublishedTextPage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(PublishedTextPage.Key.Value, updateModel, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPageId, preview);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(titleName, textPage.Value("title"));
    }

    [Test]
    [TestCase(true, "New Name")]
    [TestCase(false, "Welcome to our Home page")]
    public async Task Can_Get_Updated_Draft_Of_Published_Content_Property_By_Key(bool preview, string titleName)
    {
        // Arrange
        PublishedTextPage.InvariantProperties.First(x => x.Alias == "title").Value = titleName;
        ContentUpdateModel updateModel = new ContentUpdateModel
        {
            InvariantName = PublishedTextPage.InvariantName,
            InvariantProperties = PublishedTextPage.InvariantProperties,
            Variants = PublishedTextPage.Variants,
            TemplateKey = PublishedTextPage.TemplateKey,
        };
        await ContentEditingService.UpdateAsync(PublishedTextPage.Key.Value, updateModel, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value, true);

        // Assert
        using var contextReference = UmbracoContextFactory.EnsureUmbracoContext();
        Assert.AreEqual(titleName, textPage.Value("title"));
    }

    [Test]
    public async Task Can_Not_Get_Deleted_Content_By_Id()
    {
        // Arrange
        var content = await PublishedContentHybridCache.GetByIdAsync(Subpage3Id, true);
        Assert.IsNotNull(content);
        await ContentEditingService.DeleteAsync(Subpage3.Key.Value, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(Subpage3Id, true);

        // Assert
        Assert.IsNull(textPage);
    }

    [Test]
    public async Task Can_Not_Get_Deleted_Content_By_Key()
    {
        // Arrange
        await PublishedContentHybridCache.GetByIdAsync(Subpage3.Key.Value, true);
        var hasContent = await PublishedContentHybridCache.HasByIdAsync(Subpage3Id, true);
        Assert.IsTrue(hasContent);
        var result = await ContentEditingService.DeleteAsync(Subpage3.Key.Value, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(Subpage3.Key.Value, true);

        // Assert
        Assert.IsNull(textPage);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Can_Not_Get_Deleted_Published_Content_By_Id(bool preview)
    {
        // Arrange
        await ContentEditingService.DeleteAsync(PublishedTextPage.Key.Value, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPageId, preview);

        // Assert
        Assert.IsNull(textPage);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Can_Not_Get_Deleted_Published_Content_By_Key(bool preview)
    {
        // Arrange
        await ContentEditingService.DeleteAsync(PublishedTextPage.Key.Value, Constants.Security.SuperUserKey);

        // Act
        var textPage = await PublishedContentHybridCache.GetByIdAsync(PublishedTextPage.Key.Value, preview);

        // Assert
        Assert.IsNull(textPage);
    }

    private void AssertTextPage(IPublishedContent textPage)
    {
        Assert.Multiple(() =>
        {
            Assert.IsNotNull(textPage);
            Assert.AreEqual(Textpage.Key, textPage.Key);
            Assert.AreEqual(Textpage.ContentTypeKey, textPage.ContentType.Key);
            Assert.AreEqual(Textpage.InvariantName, textPage.Name);
        });

        AssertProperties(Textpage.InvariantProperties, textPage.Properties);
    }

    private void AssertPublishedTextPage(IPublishedContent textPage)
    {
        Assert.Multiple(() =>
        {
            Assert.IsNotNull(textPage);
            Assert.AreEqual(PublishedTextPage.Key, textPage.Key);
            Assert.AreEqual(PublishedTextPage.ContentTypeKey, textPage.ContentType.Key);
            Assert.AreEqual(PublishedTextPage.InvariantName, textPage.Name);
        });

        AssertProperties(PublishedTextPage.InvariantProperties, textPage.Properties);
    }

    private void AssertProperties(IEnumerable<PropertyValueModel> propertyCollection, IEnumerable<IPublishedProperty> publishedProperties)
    {
        foreach (var prop in propertyCollection)
        {
            AssertProperty(prop, publishedProperties.First(x => x.Alias == prop.Alias));
        }
    }

    private void AssertProperty(PropertyValueModel property, IPublishedProperty publishedProperty)
    {
        Assert.Multiple(() =>
        {
            Assert.AreEqual(property.Alias, publishedProperty.Alias);
            Assert.AreEqual(property.Value, publishedProperty.GetSourceValue());
        });
    }
}