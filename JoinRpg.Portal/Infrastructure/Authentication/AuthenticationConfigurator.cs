using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Portal.Infrastructure
{
    internal static class AuthenticationConfigurator
    {
        public static void ConfigureJoinExternalLogins(this AuthenticationBuilder authBuilder, IConfigurationSection configSection)
        {
            var googleConfig = configSection.GetSection("Google").Get<GoogleAuthenticationOptions>();

            if (googleConfig is object)
            {
                authBuilder.AddGoogle(options =>
                {
                    options.SignInScheme = IdentityConstants.ExternalScheme;

                    (options.ClientId, options.ClientSecret) = googleConfig;
                });
            }
        }
    }
}
