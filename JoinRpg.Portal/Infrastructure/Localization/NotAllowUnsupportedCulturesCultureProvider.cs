using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace JoinRpg.Portal.Infrastructure.Localization
{
    public class NotAllowUnsupportedCulturesCultureProvider : RequestCultureProvider
    {
        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException();
            }

            var cultureFromCookie = context
                .Request
                .Cookies[CookieRequestCultureProvider.DefaultCookieName]
                .ToString()
                .Split('|')
                .FirstOrDefault()
                .Split('=')
                .ElementAtOrDefault(1);

            if (IsSupported(cultureFromCookie))
            {
                return Task.FromResult(new ProviderCultureResult(cultureFromCookie));
            }

            string cultureFromRequestParam = context.Request.Query["culture"];

            if (IsSupported(cultureFromRequestParam))
            {
                return Task.FromResult(new ProviderCultureResult(cultureFromRequestParam));
            }

            var firstUserLanguageCulture = context.Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault();

            if (IsSupported(firstUserLanguageCulture))
            {
                return Task.FromResult(new ProviderCultureResult(firstUserLanguageCulture));
            }

            string defaultEmptyCulture = null;
            return Task.FromResult(new ProviderCultureResult(defaultEmptyCulture));
        }

        private bool IsSupported(string culture)
        {
            return !string.IsNullOrEmpty(culture)
                && !LocalizationService.SupportedCultures.Contains(new CultureInfo(culture));

        }
    }
}
