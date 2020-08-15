using System;
using System.Threading.Tasks;
using Joinrpg.Web.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Infrastructure
{
    internal static class AuthenticationConfigurator
    {
        public static void ConfigureJoinExternalLogins(this AuthenticationBuilder authBuilder, IConfigurationSection configSection)
        {
            var googleConfig = configSection.GetSection("Google").Get<OAuthAuthenticationOptions>();

            if (googleConfig is object)
            {
                authBuilder.AddGoogle(options =>
                {
                    options.SignInScheme = IdentityConstants.ExternalScheme;

                    (options.ClientId, options.ClientSecret) = googleConfig;
                });
            }

            var vkConfig = configSection.GetSection("Vkontakte").Get<OAuthAuthenticationOptions>();

            if (vkConfig is object)
            {
                authBuilder.AddVkontakte(options =>
                {
                    options.SignInScheme = IdentityConstants.ExternalScheme;

                    (options.ClientId, options.ClientSecret) = vkConfig;
                });
            }
        }


        public static void ConfigureValidation(this PasswordOptions password)
        {
            password.RequiredLength = 8;
            password.RequireLowercase = false;
            password.RequireUppercase = false;
            password.RequireNonAlphanumeric = false;
            password.RequireDigit = false;
        }

        public static Action<CookieAuthenticationOptions> SetCookieOptions() => options =>
        {
            options.Events.OnRedirectToAccessDenied =
               options.Events.OnRedirectToLogin = OnCookieRedirect;
        };

        private static Task OnCookieRedirect(RedirectContext<CookieAuthenticationOptions> context)
        {
            if (context.Request.Path.Value.IsApiPath())
            {
                context.Response.StatusCode = 401;
            }
            else
            {
                context.Response.Redirect(context.RedirectUri);

            }
            return Task.CompletedTask;
        }
    }
}
