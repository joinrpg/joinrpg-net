using System;
using System.Threading.Tasks;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Joinrpg.Web.Identity;

namespace JoinRpg.Portal.Infrastructure
{
    internal static class AuthenticationConfigurator
    {
        public static IServiceCollection AddJoinAuth(this IServiceCollection services,
            JwtSecretOptions jwtSecretOptions,
            IWebHostEnvironment environment,
            IConfigurationSection authSection)
        {

            _ = services.Configure<PasswordHasherOptions>(options => options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2);

            _ = services
                .AddIdentity<JoinIdentityUser, string>(options => options.Password.ConfigureValidation())
                .AddDefaultTokenProviders()
                .AddUserStore<MyUserStore>()
                .AddRoleStore<MyUserStore>();

            _ = services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN-HEADERNAME");

            _ = services.ConfigureApplicationCookie(SetCookieOptions());

            _ = services.AddAuthorization(o => o.DefaultPolicy = new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme,
                IdentityConstants.ApplicationScheme
                )
                .RequireAuthenticatedUser()
              .Build())
                .AddTransient<IAuthorizationPolicyProvider, AuthPolicyProvider>()
                .AddAuthentication()
                .ConfigureJoinExternalLogins(authSection)
                .AddJwtBearer(o =>
                {
                    o.RequireHttpsMetadata = !environment.IsDevelopment();
                    o.SaveToken = false;
                    o.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretOptions.SecretKey));
                    o.TokenValidationParameters.ValidAudience = "ApiUser";
                    o.TokenValidationParameters.ValidIssuer = jwtSecretOptions.Issuer;
                });

            return services;
        }

        public static AuthenticationBuilder ConfigureJoinExternalLogins(this AuthenticationBuilder authBuilder, IConfigurationSection configSection)
        {
            var googleConfig = configSection.GetSection("Google").Get<OAuthAuthenticationOptions>();

            if (googleConfig is object)
            {
                _ = authBuilder.AddGoogle(options =>
                  {
                      options.ClaimActions.MapJsonKey("urn:google:photo", "picture");

                      SetCommonProperties(options, googleConfig);
                  });
            }

            var vkConfig = configSection.GetSection("Vkontakte").Get<OAuthAuthenticationOptions>();

            if (vkConfig is object)
            {
                _ = authBuilder.AddVkontakte(options =>
                  {
                      options.Scope.Add("email");

                      SetCommonProperties(options, vkConfig);
                  });
            }

            return authBuilder;

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
