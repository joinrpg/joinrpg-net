using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using JoinRpg.Portal.Resources;
using Microsoft.Extensions.Localization;

namespace JoinRpg.Portal.Infrastructure.Localization
{
    public class LocalizationService
    {
        private readonly IStringLocalizer _localizer;
        public static IList<CultureInfo> SupportedCultures { get; } = new ReadOnlyCollection<CultureInfo>
                    (
                        new List<CultureInfo>()
                        {
                            new CultureInfo("ru-RU"),
                            new CultureInfo("ru"),
                            new CultureInfo("en-US"),
                            new CultureInfo("en"),
                            new CultureInfo("de-DE"),
                            new CultureInfo("de")
                        }
                    );
        public string this[string key]
        {
            get { return _localizer[key]; }
        }

        public LocalizationService(IStringLocalizerFactory factory)
        {
            _localizer = CreateLocalizer(factory);
        }

        public static IStringLocalizer CreateLocalizer(IStringLocalizerFactory factory)
        {
            var assemblyName = new AssemblyName(typeof(LocalizationSharedResource).GetTypeInfo().Assembly.FullName);
            return factory.Create("LocalizationSharedResource", assemblyName.Name);
        }

        ///<summary>
        ///   Gets the string from the localizer by given key.
        ///   If the obtained string is null or not found(e.g.equals to key itself),
        ///   the default value is returned.
        ///</summary>
        public static string GetLocalizedString(IStringLocalizer localizer, string key, string defaultValue)
        {
            string localizedString = localizer[key];
            return localizedString == null || localizedString.Equals(key) ? defaultValue : localizedString;
        }

        public static string GenerateLocalizationKey<ModelType, AttributeType>(string memberName, string attributePropertyName)
        {
            return GenerateLocalizationKey(typeof(ModelType), typeof(AttributeType), memberName, attributePropertyName);
        }

        public static string GenerateLocalizationKey(Type modelType, Type attributeType, string memberName, string attributePropertyName)
        {
            var components = new[] {
                modelType.FullName,
                memberName,
                attributeType.Name.Replace("Attribute", ""),
                attributePropertyName
            };

            return string.Join(".", components.Where(x => x != null));
        }
    }
}
