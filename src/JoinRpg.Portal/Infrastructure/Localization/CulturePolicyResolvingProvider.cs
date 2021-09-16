using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using MoreLinq;

namespace JoinRpg.Portal.Infrastructure.Localization
{
    public class CulturePolicyResolvingProvider : RequestCultureProvider
    {
        private readonly static Task<ProviderCultureResult> DefaultCulture;

        static CulturePolicyResolvingProvider()
        {
            var defaultCulture = LocalizationService.SupportedCultures.ElementAtOrDefault(0);
            var defaultCultureName = defaultCulture?.Name ?? "ru";
            DefaultCulture = Task.FromResult(new ProviderCultureResult(defaultCultureName));
        }

        public override Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException();
            }

            return FromRequestParameter(context) ?? FromCookie(context) ?? FromHeader(context) ?? DefaultCulture;
        }

        private Task<ProviderCultureResult> FromHeader(HttpContext context)
        {
            List<CultureInfo> culturesFromHeader;
            try
            {
                culturesFromHeader = GetCulturesFromHeader(context);
            }
            catch (CultureNotFoundException ex)
            {
                return null;
            }

            foreach (CultureInfo culture in culturesFromHeader)
            {
                if (IsSupported(culture))
                {
                    return Task.FromResult(new ProviderCultureResult(culture.ToString()));
                }
            }

            return null;
        }

        private Task<ProviderCultureResult> FromCookie(HttpContext context)
        {
            CultureInfo cultureFromCookie = null;
            try
            {
                cultureFromCookie = GetCultureFromCookie(context);
            }
            catch (CultureNotFoundException ex)
            {
                return null;
            }

            return IsSupported(cultureFromCookie)
                ? Task.FromResult(new ProviderCultureResult(cultureFromCookie.ToString()))
                : null;
        }

        private Task<ProviderCultureResult> FromRequestParameter(HttpContext context)
        {
            try
            {
                var cultureFromRequestParam = context.Request.Query.ContainsKey("culture")
                    ? context.Request.Query["culture"].ToString()
                    : null;

                return cultureFromRequestParam != null && IsSupported(new CultureInfo(cultureFromRequestParam))
                     ? Task.FromResult(new ProviderCultureResult(cultureFromRequestParam))
                     : null;
            }
            catch (CultureNotFoundException ex)
            {
                return null;
            }
        }

        private CultureInfo GetCultureFromCookie(HttpContext context)
        {
            if (context == null)
            {
                return null;
            }

            if (!context.Request.Cookies.ContainsKey(CookieRequestCultureProvider.DefaultCookieName))
            {
                return null;
            }

            string culture = context
               .Request
               .Cookies[CookieRequestCultureProvider.DefaultCookieName]?
               .ToString()
               .Split('|')
               .FirstOrDefault()
               .Split('=')
               .ElementAtOrDefault(1);

            return culture != null ? new CultureInfo(culture) : null;
        }

        private List<CultureInfo> GetCulturesFromHeader(HttpContext context)
        {
            return context
                .Request
                .Headers["Accept-Language"]
                .ToString()
                .Split(';')
                .Select(cultureName => cultureName.Split(','))
                .Select(cultureName => cultureName.ElementAtOrDefault(1))
                .Where(cultureName => !string.IsNullOrEmpty(cultureName))
                .Select(cultureName => cultureName.Trim())
                .Where(cultureName => !"*".Equals(cultureName))
                .Select(item => new CultureInfo(item))
                .ToList();
        }

        private bool IsSupported(CultureInfo cultureInfo)
        {
            return cultureInfo != null && LocalizationService.SupportedCultures.Contains(cultureInfo);
        }
    }
}
