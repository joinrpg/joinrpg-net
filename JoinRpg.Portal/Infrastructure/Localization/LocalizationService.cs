using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using JoinRpg.Portal.Resources;
using JoinRpg.Web.Models;
using Microsoft.Extensions.Localization;

namespace JoinRpg.Portal.Infrastructure.Localization
{

    public class LocalizationService
    {
        private readonly IStringLocalizer _localizer;

        public string this[string key]
        {
            get { return _localizer[key];  }
        }

        public static IStringLocalizer CreateLocalizer(IStringLocalizerFactory factory)
        {
            var assemblyName = new AssemblyName(typeof(LocalizationSharedResource).GetTypeInfo().Assembly.FullName);
            return factory.Create("LocalizationSharedResource", assemblyName.Name);
        }
        public LocalizationService(IStringLocalizerFactory factory)
        {
            _localizer = CreateLocalizer(factory);
        }
    }
}
