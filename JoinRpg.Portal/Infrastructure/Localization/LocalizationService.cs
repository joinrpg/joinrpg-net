using JoinRpg.Portal.Resources;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;

namespace JoinRpg.Portal.Infrastructure.Localization
{
    public class LocalizationService
    {
        private readonly IStringLocalizer _localizer;
        public static IList<CultureInfo> SupportedCultures { get; } = new ReadOnlyCollection<CultureInfo>
                    (
                        new List<CultureInfo>()
                        {
                            new CultureInfo("en-US"),
                            new CultureInfo("ru-RU"),
                            new CultureInfo("de-DE"),
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
    }
}
