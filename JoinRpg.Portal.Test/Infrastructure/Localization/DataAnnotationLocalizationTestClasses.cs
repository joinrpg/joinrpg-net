using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using JoinRpg.Portal.Infrastructure.Localization;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Test.Infrastructure.Localization
{
    // --------------------- default attributes ---------------------------
    /**
       * Validation attributes:
       *
       * - CreditCard
       * - CustomValidation
       * - Compare
       * - DataType
       * - EmailAddress
       * - MinLength
       * - MaxLength
       * - Phone
       * - Range
       * - RegularExpression
       * - Required
       * - StringLength
       * - Url
       * - Remote
       *
       * Other attributes:
       *
       * - Display
       * - DisplayName
       * - DisplayFormat
       * - Description
       */

    // --------------------------------------------------------------------
    // Test localizer
    internal class TestLocalizer : IStringLocalizer
    {
        private Dictionary<string, LocalizedString> _localization;

        public TestLocalizer(Dictionary<string, LocalizedString> localization)
        {
            _localization = localization;
        }

        public LocalizedString this[string name] =>
            name == null ? null : (_localization.ContainsKey(name) ? _localization[name] : new LocalizedString("name", name));

        public LocalizedString this[string name, params object[] arguments] =>
            name == null ? null : (_localization.ContainsKey(name) ? _localization[name] : new LocalizedString("name", name));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            throw new NotImplementedException();
        }

        public IStringLocalizer WithCulture(CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Test options
    internal class TestOptions : IOptions<MvcDataAnnotationsLocalizationOptions>
    {
        public TestOptions()
        {
            Value = new MvcDataAnnotationsLocalizationOptions()
            {
                DataAnnotationLocalizerProvider = (type, factory) => factory.Create(type)
            };
        }

        public MvcDataAnnotationsLocalizationOptions Value { get; }
    }

    // Test factory
    internal class TestStringLocalizerFactory : IStringLocalizerFactory
    {
        private IStringLocalizer _localizer;

        public TestStringLocalizerFactory(Dictionary<string, LocalizedString> localization)
        {
            _localizer = new TestLocalizer(localization);
        }
        public IStringLocalizer Create(string baseName, string location)
        {
            return _localizer;
        }

        public IStringLocalizer Create(Type resourceSource)
        {
            return _localizer;
        }
    }
    internal class DisplayAttributeTestClass
    {
        //name localization + ----> !
        //short name localization +
        //display name localization +
        //name default +
        //short name default +
        //display name default +

        //description localization + ---> !
        //descattr localization +
        //description default +
        //descattr default +

        //prompt localization + ---> !
        //prompt default +
        [Display(Name = "N", ShortName = "SN", Description = "D", Prompt = "P")]
        [DisplayName("DN")]
        [Description("DD")]
        public string PropertyWithNameLocalization { get; set; }

        //name localization -
        //short name localization + ----> !
        //display name localization +
        //name default +
        //short name default +
        //display name default +

        //description localization -
        //descattr localization + ----> !
        //description default +
        //descattr default +

        //prompt localization -
        //prompt default + ----> !
        [Display(Name = "N", ShortName = "SN", Description = "D", Prompt = "P")]
        [DisplayName("DN")]
        [Description("DD")]
        public string PropertyWithShortNameLocalization { get; set; }

        //name localization -
        //short name localization -
        //display name localization + ----> !
        //name default +
        //short name default +
        //display name default +

        //description localization -
        //descattr localization -
        //description default + --->! 
        //descattr default +

        //prompt localization -
        //prompt default -
        [Display(Name = "N", ShortName = "SN", Description = "D")]
        [DisplayName("DN")]
        [Description("DD")]
        public string PropertyWithDisplayNameLocalization { get; set; }

        //name localization -
        //short name localization -
        //display name localization -
        //name default + ---->!
        //short name default +
        //display name default +

        //description localization -
        //descattr localization -
        //description default -
        //descattr default + ---->!
        [Display(Name = "N", ShortName = "SN", Prompt = "P")]
        [DisplayName("DN")]
        [Description("DD")]
        public string PropertyWithNameDefault { get; set; }

        //name localization -
        //short name localization -
        //display name localization -
        //name default -
        //short name default + ----> !
        //display name default +

        //description localization -
        //descattr localization -
        //description default -
        //descattr default -
        [Display(ShortName = "SN", Prompt = "P")]
        [DisplayName("DN")]
        [Description]
        public string PropertyWithShortNameDefault { get; set; }

        //name localization -
        //short name localization -
        //display name localization -
        //name default -
        //short name default -
        //display name default + -----> !
        [Display(Description = "D", Prompt = "P")]
        [DisplayName("DN")]
        [Description("DD")]
        public string PropertyWithDisplayNameDefault { get; set; }

        //name localization -
        //short name localization -
        //display name localization -
        //name default -
        //short name default -
        //display name default -
        [Display]
        [DisplayName]
        [Description]
        public string PropertyWithoutLocalizationAndDefaults { get; set; }

        //name localization +
        //short name localization +
        //display name localization + ---> !
        //name default -
        //short name default -
        //display name default +

        //description localization +
        //descattr localization + ----> !
        //description default +
        //descattr default +
        [DisplayName("DN")]
        [Description("DD")]
        public string PropertyWithDisplayNameAndDescriptionAttributeLocalization { get; set; }

        //name localization + ----> !
        //short name localization +
        //display name localization +
        //name default +
        //short name default +
        //display name default -

        //description localization + ----> !
        //descattr localization + 
        //description default +
        //descattr default +
        [Display(Name = "N", ShortName = "SN", Description = "D", Prompt = "P")]
        [Description("DD")]
        public string PropertyWithNameLocalizationAndDescriptionAttribute { get; set; }

        //name localization + ----> !
        //short name localization +
        //display name localization +
        //name default -
        //short name default -
        //display name default -

        //description localization + ---> !
        //descattr localization +
        //description default -
        //descattr default -
        [Display]
        [DisplayName]
        [Description]
        public string PropertyWithLocalizationAndNoDefaults { get; set; }

        //name localization +
        //short name localization +
        //display name localization + ---> !
        //name default -
        //short name default -
        //display name default -

        //description localization +
        //descattr localization + ----> !
        //description default -
        //descattr default -
        [DisplayName]
        [Description]
        public string PropertyWithEmptyDisplayAndDescriptionAttributes { get; set; }

        //name localization +
        //short name localization +
        //display name localization + 
        //name default -
        //short name default -
        //display name default -

        //description localization +
        //descattr localization +
        //description default -
        //descattr default -
        public string PropertyWithoutAttributes { get; set; }
    }

    internal class DataAnnotationsLocalizerTestContext
    {
        private TestStringLocalizerFactory _factory;
        private IOptions<MvcDataAnnotationsLocalizationOptions> _options;
        public Dictionary<string, LocalizedString> _localization;

        public DataAnnotationsLocalizationDisplayMetadataProvider Provider { get; }

        public DataAnnotationsLocalizerTestContext(bool setupFullLocalization)
        {
            _localization = new Dictionary<string, LocalizedString>();
            _factory = new TestStringLocalizerFactory(_localization);
            _options = new TestOptions();

            Provider = new DataAnnotationsLocalizationDisplayMetadataProvider(_options, _factory);

            if (setupFullLocalization)
            {
                SetupFullLocalization();
            }
        }

        public void RemoveLocalizationEntry<AttributeType>(string memberName, string attributePropertyName)
        {
            _localization.Remove(
                GenerateKey(
                    typeof(DisplayAttributeTestClass),
                    typeof(AttributeType),
                    memberName,
                    attributePropertyName
                    )
                );
        }

        private string GenerateKey(Type objectType, Type attrType, string memberName, string attributeProperty)
        {
            return objectType.FullName.Replace("+", ".") + "."
                + memberName + "."
                + attrType.Name.Replace("Attribute", "") + "."
                + attributeProperty;
        }
        private string GenerateKey<Type, AttributeType>(string memberName, string attributePropertyName)
        {
            return GenerateKey(typeof(Type), typeof(AttributeType), memberName, attributePropertyName);
        }

        private void SetupFullLocalization()
        {
            #region PropertyWithNameLocalization
            //name localization + ----> !
            //short name localization +
            //display name localization +
            //name default +
            //short name default +
            //display name default +

            //description localization + ---> !
            //descattr localization +
            //description default +
            //descattr default +

            //prompt localization + ---> !
            //prompt default +

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                    nameof(DisplayAttributeTestClass.PropertyWithNameLocalization),
                    nameof(DisplayAttribute.Name)),
                new LocalizedString("name",
                    nameof(DisplayAttributeTestClass.PropertyWithNameLocalization) + nameof(DisplayAttribute.Name))
                );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization),
                   nameof(DisplayAttribute.ShortName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization) + nameof(DisplayAttribute.ShortName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization),
                   nameof(DisplayAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization) + nameof(DisplayAttribute.Description))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization),
                   nameof(DisplayAttribute.Prompt)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization) + nameof(DisplayAttribute.Prompt))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayNameAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization),
                   nameof(DisplayNameAttribute.DisplayName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization) + nameof(DisplayNameAttribute.DisplayName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DescriptionAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization),
                   nameof(DescriptionAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalization) + nameof(DescriptionAttribute.Description))
               );
            #endregion
            #region PropertyWithShortNameLocalization

            //name localization -
            //short name localization + ----> !
            //display name localization +
            //name default +
            //short name default +
            //display name default +

            //description localization -
            //descattr localization + ----> !
            //description default +
            //descattr default +

            //prompt localization -
            //prompt default + ----> !

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                    nameof(DisplayAttributeTestClass.PropertyWithShortNameLocalization),
                    nameof(DisplayAttribute.ShortName)
                    ), new LocalizedString(
                    "name",
                    nameof(DisplayAttributeTestClass.PropertyWithShortNameLocalization) + nameof(DisplayAttribute.ShortName)
                    )
                );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayNameAttribute>(
                    nameof(DisplayAttributeTestClass.PropertyWithShortNameLocalization),
                    nameof(DisplayNameAttribute.DisplayName)
                    ), new LocalizedString(
                   "name",
                   nameof(DisplayAttributeTestClass.PropertyWithShortNameLocalization) + nameof(DisplayNameAttribute.DisplayName)
                   )
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DescriptionAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithShortNameLocalization),
                   nameof(DescriptionAttribute.Description)
                   ), new LocalizedString(
                   "name",
                   nameof(DisplayAttributeTestClass.PropertyWithShortNameLocalization) + nameof(DescriptionAttribute.Description)
                   )
               );
            #endregion
            #region PropertyWithDisplayNameLocalization
            //name localization -
            //short name localization -
            //display name localization + ----> !
            //name default +
            //short name default +
            //display name default +

            //description localization -
            //descattr localization -
            //description default + --->! 
            //descattr default +

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayNameAttribute>(
                    nameof(DisplayAttributeTestClass.PropertyWithDisplayNameLocalization),
                    nameof(DisplayNameAttribute.DisplayName)
                    ), new LocalizedString(
                   "name",
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameLocalization) + nameof(DisplayNameAttribute.DisplayName)
                   )
               );
            #endregion
            #region PropertyWithDisplayNameAndDescriptionAttributeLocalization

            //name localization +
            //short name localization +
            //display name localization + ---> !
            //name default -
            //short name default -
            //display name default +

            //description localization +
            //descattr localization + ----> !
            //description default +
            //descattr default +

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization),
                   nameof(DisplayAttribute.Name)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization) + nameof(DisplayAttribute.Name))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization),
                   nameof(DisplayAttribute.ShortName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization) + nameof(DisplayAttribute.ShortName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization),
                   nameof(DisplayAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization) + nameof(DisplayAttribute.Description))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                  nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization),
                  nameof(DisplayAttribute.Prompt)),
              new LocalizedString("name",
                  nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization) + nameof(DisplayAttribute.Prompt))
              );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayNameAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization),
                   nameof(DisplayNameAttribute.DisplayName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization) + nameof(DisplayNameAttribute.DisplayName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DescriptionAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization),
                   nameof(DescriptionAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithDisplayNameAndDescriptionAttributeLocalization) + nameof(DescriptionAttribute.Description))
               );
            #endregion
            #region PropertyWithNameLocalizationAndDescriptionAttribute

            //name localization + ----> !
            //short name localization +
            //display name localization +
            //name default +
            //short name default +
            //display name default -

            //description localization + ----> !
            //descattr localization + 
            //description default +
            //descattr default +

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute),
                   nameof(DisplayAttribute.Name)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute) + nameof(DisplayAttribute.Name))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute),
                   nameof(DisplayAttribute.ShortName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute) + nameof(DisplayAttribute.ShortName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute),
                   nameof(DisplayAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute) + nameof(DisplayAttribute.Description))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute),
                   nameof(DisplayAttribute.Prompt)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute) + nameof(DisplayAttribute.Prompt))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayNameAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute),
                   nameof(DisplayNameAttribute.DisplayName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute) + nameof(DisplayNameAttribute.DisplayName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DescriptionAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute),
                   nameof(DescriptionAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithNameLocalizationAndDescriptionAttribute) + nameof(DescriptionAttribute.Description))
               );
            #endregion
            #region PropertyWithLocalizationAndNoDefaults
            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                    nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults),
                    nameof(DisplayAttribute.Name)),
                new LocalizedString("name",
                    nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults) + nameof(DisplayAttribute.Name))
                );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults),
                   nameof(DisplayAttribute.ShortName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults) + nameof(DisplayAttribute.ShortName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults),
                   nameof(DisplayAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults) + nameof(DisplayAttribute.Description))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults),
                   nameof(DisplayAttribute.Prompt)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults) + nameof(DisplayAttribute.Prompt))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayNameAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults),
                   nameof(DisplayNameAttribute.DisplayName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults) + nameof(DisplayNameAttribute.DisplayName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DescriptionAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults),
                   nameof(DescriptionAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithLocalizationAndNoDefaults) + nameof(DescriptionAttribute.Description))
               );
            #endregion
            #region PropertyWithEmptyDisplayAndDescriptionAttributes
            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                    nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes),
                    nameof(DisplayAttribute.Name)),
                new LocalizedString("name",
                    nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes) + nameof(DisplayAttribute.Name))
                );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes),
                   nameof(DisplayAttribute.ShortName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes) + nameof(DisplayAttribute.ShortName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes),
                   nameof(DisplayAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes) + nameof(DisplayAttribute.Description))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes),
                   nameof(DisplayAttribute.Prompt)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes) + nameof(DisplayAttribute.Prompt))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayNameAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes),
                   nameof(DisplayNameAttribute.DisplayName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes) + nameof(DisplayNameAttribute.DisplayName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DescriptionAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes),
                   nameof(DescriptionAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithEmptyDisplayAndDescriptionAttributes) + nameof(DescriptionAttribute.Description))
               );
            #endregion
            #region PropertyWithoutAttributes
            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                    nameof(DisplayAttributeTestClass.PropertyWithoutAttributes),
                    nameof(DisplayAttribute.Name)),
                new LocalizedString("name",
                    nameof(DisplayAttributeTestClass.PropertyWithoutAttributes) + nameof(DisplayAttribute.Name))
                );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes),
                   nameof(DisplayAttribute.ShortName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes) + nameof(DisplayAttribute.ShortName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes),
                   nameof(DisplayAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes) + nameof(DisplayAttribute.Description))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes),
                   nameof(DisplayAttribute.Prompt)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes) + nameof(DisplayAttribute.Prompt))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DisplayNameAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes),
                   nameof(DisplayNameAttribute.DisplayName)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes) + nameof(DisplayNameAttribute.DisplayName))
               );

            _localization.Add(GenerateKey<DisplayAttributeTestClass, DescriptionAttribute>(
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes),
                   nameof(DescriptionAttribute.Description)),
               new LocalizedString("name",
                   nameof(DisplayAttributeTestClass.PropertyWithoutAttributes) + nameof(DescriptionAttribute.Description))
               );
            #endregion

        }
    }
}
