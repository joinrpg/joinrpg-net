using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Xunit;

namespace JoinRpg.Portal.Test.Infrastructure.Localization
{
    public class DataAnnotationsLocalizationDisplayMetadataProviderTest
    {
        private DataAnnotationsLocalizerTestContext _context;
        public DataAnnotationsLocalizationDisplayMetadataProviderTest()
        {
            _context = new DataAnnotationsLocalizerTestContext(true);
        }

        [Fact]
        public void CreateDisplayMetadata_WithDisplayAttributeNameAndDescriptionAndPromptLocalization_ShouldLocalizeNameAndDescriptionAndPrompt()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithNameLocalization);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyName + nameof(DisplayAttribute.Name), context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyName + nameof(DisplayAttribute.Description), context.DisplayMetadata.Description());
            Assert.Equal(propertyName + nameof(DisplayAttribute.Prompt), context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithShortNameAndDescriptionAttributeLocalizationAndWithoutPromptLocalization_ShouldLocalizeShortNameAndDescriptionAndSetPromptToDefault()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithShortNameLocalization);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyName + nameof(DisplayAttribute.ShortName), context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyName + nameof(DescriptionAttribute.Description), context.DisplayMetadata.Description());
            Assert.Equal(propertyInfo.GetAttribute<DisplayAttribute>().Prompt, context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithDisplayNameLocalizationAndDefaultDescriptionAndNoPrompt_ShouldLocalizeDisplayNameAndSetDefaultDescriptionAndNullPrompt()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithDisplayNameLocalization);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyName + nameof(DisplayNameAttribute.DisplayName), context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyInfo.GetAttribute<DisplayAttribute>().Description, context.DisplayMetadata.Description());
            Assert.Null(context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithNoLocalizationOfDisplayName_ShouldTakeDefaultValuesOfNameDescriptionAndPrompt()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithNameDefault);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyInfo.GetAttribute<DisplayAttribute>().Name, context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyInfo.GetAttribute<DescriptionAttribute>().Description, context.DisplayMetadata.Description());
            Assert.Equal(propertyInfo.GetAttribute<DisplayAttribute>().Prompt, context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithNoLocalizationOfDisplayShortName_ShouldTakeDefaultValuesOfShortNameDescriptionAndPrompt()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithShortNameDefault);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyInfo.GetAttribute<DisplayAttribute>().ShortName, context.DisplayMetadata.DisplayName());
            Assert.Equal("", context.DisplayMetadata.Description());
            Assert.Equal(propertyInfo.GetAttribute<DisplayAttribute>().Prompt, context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithNoLocalizationOfDisplayName_ShouldSetDisplayNameDefaultValue()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithDisplayNameDefault);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyInfo.GetAttribute<DisplayNameAttribute>().DisplayName, context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyInfo.GetAttribute<DisplayAttribute>().Description, context.DisplayMetadata.Description());
            Assert.Equal(propertyInfo.GetAttribute<DisplayAttribute>().Prompt, context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithNoLocalizationAndWithNoDefaults_ShouldSetAllValuesToNull()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithoutLocalizationAndDefaults);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal("", context.DisplayMetadata.DisplayName());
            Assert.Equal("", context.DisplayMetadata.Description());
            Assert.Null(context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithLocalizationOfDisplayNameAndDescriptionWithoutDisplayAttribute_ShouldLocalizeDisplayNameAndDescription()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyName + nameof(DisplayNameAttribute.DisplayName), context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyName + nameof(DescriptionAttribute.Description), context.DisplayMetadata.Description());
        }

        [Fact]
        public void CreateDisplayMetadata_WithLocalizationOfDisplayNameAndDescriptionWithoutDisplayNameAttribute_ShouldLocalizeDisplayNameAndDescription()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyName + nameof(DisplayAttribute.Name), context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyName + nameof(DescriptionAttribute.Description), context.DisplayMetadata.Description());
            Assert.Equal(propertyName + nameof(DisplayAttribute.Prompt), context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithAllAttributesAndLocalizationButWithoutDefaults_ShouldLocalizeNameAndDescriptionAndPrompt()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyName + nameof(DisplayAttribute.Name), context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyName + nameof(DisplayAttribute.Description), context.DisplayMetadata.Description());
            Assert.Equal(propertyName + nameof(DisplayAttribute.Prompt), context.DisplayMetadata.Placeholder());
        }

        [Fact]
        public void CreateDisplayMetadata_WithLocalizationOfDisplayNameAndDescriptionButWithoutDefaults_ShouldLocalizeDisplayNameAndDescription()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Equal(propertyName + nameof(DisplayNameAttribute.DisplayName), context.DisplayMetadata.DisplayName());
            Assert.Equal(propertyName + nameof(DescriptionAttribute.Description), context.DisplayMetadata.Description());
        }

        [Fact]
        public void CreateDisplayMetadata_WithNoAttributes_ShouldSetAllValuesToNull()
        {
            //arrange
            var type = typeof(DisplayAttributeTestClass);
            var propertyName = nameof(DisplayAttributeTestClass.PropertyWithoutAttributes);
            var propertyInfo = type.GetProperty(propertyName);
            DisplayMetadataProviderContext context = new DisplayMetadataProviderContext(
                ModelMetadataIdentity.ForProperty(propertyInfo, type, typeof(string)),
                ModelAttributes.GetAttributesForProperty(type, propertyInfo));
            //action
            _context.Provider.CreateDisplayMetadata(context);
            //assert
            Assert.Null(context.DisplayMetadata.DisplayName);
            Assert.Null(context.DisplayMetadata.Description);
            Assert.Null(context.DisplayMetadata.Placeholder);
        }
    }
}
