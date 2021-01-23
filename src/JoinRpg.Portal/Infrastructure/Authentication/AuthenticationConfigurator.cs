using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
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
                    options.ClaimActions.MapJsonKey("urn:google:photo", "picture");

                    SetCommonProperties(options, googleConfig);
                });
            }

            var vkConfig = configSection.GetSection("Vkontakte").Get<OAuthAuthenticationOptions>();

            if (vkConfig is object)
            {
                authBuilder.AddVkontakte(options =>
                {
                    options.Scope.Add("email");

                    SetCommonProperties(options, vkConfig);
                });
            }

            static void SetCommonProperties(OAuthOptions options, OAuthAuthenticationOptions config)
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;

                (options.ClientId, options.ClientSecret) = config;
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
