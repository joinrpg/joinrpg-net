using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using JoinRpg.DataModel;
using JoinRpg.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.DataProtection;
using Microsoft.Owin.Security.Google;
using Microsoft.Owin.Security.OAuth;
using Owin;
using Owin.Security.Providers.VKontakte;

namespace JoinRpg.Web
{
  public partial class Startup
  {
    internal static IDataProtectionProvider DataProtectionProvider { get; private set; }

    
    private void ConfigureAuth(IAppBuilder app)
    {
      DataProtectionProvider = app.GetDataProtectionProvider();
      app.CreatePerOwinContext(() => DependencyResolver.Current.GetService<ApplicationUserManager>());
      app.CreatePerOwinContext(() => DependencyResolver.Current.GetService<ApplicationSignInManager>());

      RegisterCookieAuth(app);

      RegisterExternalAuth(app);

      RegisterApiAuth(app);
    }

    private void RegisterApiAuth(IAppBuilder app)
    {
      var oAuthOptions = new OAuthAuthorizationServerOptions
      {
        TokenEndpointPath = new PathString("/x-api/Token"),
        Provider = new ApiSignInProvider(() => DependencyResolver.Current.GetService<ApplicationUserManager>()),
        AuthorizeEndpointPath = new PathString("/x-api/Account/ExternalLogin"),
        AccessTokenExpireTimeSpan = TimeSpan.FromDays(30),
        //TODO[SSL]
        AllowInsecureHttp = true,
      };

      // Enable the application to use bearer tokens to authenticate users
      app.UseOAuthBearerTokens(oAuthOptions);
    }

    private static void RegisterCookieAuth(IAppBuilder app)
    {
// Enable the application to use a cookie to store information for the signed in user
      // and to use a cookie to temporarily store information about a user logging in with a third party login provider
      // Configure the sign in cookie
      app.UseCookieAuthentication(new CookieAuthenticationOptions
      {
        AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
        LoginPath = new PathString("/Account/Login"),
        Provider = new CookieAuthenticationProvider
        {
          OnApplyRedirect = ctx =>
          {
            //Do not redirect to login page for failed login requests
            if (!IsApiRequest(ctx.Request))
            {
              ctx.Response.Redirect(ctx.RedirectUri);
            }
          },
          // Enables the application to validate the security stamp when the user logs in.
          // This is a security feature which is used when you change a password or add an external login to your account.  
          OnValidateIdentity = SecurityStampValidator
            .OnValidateIdentity<ApplicationUserManager, User, int>
            (validateInterval: TimeSpan.FromDays(30),
              regenerateIdentityCallback: (manager, user) => user.GenerateUserIdentityAsync(manager,
                DefaultAuthenticationTypes.ApplicationCookie),
              getUserIdCallback: claimsIdentity => claimsIdentity.GetUserId<int>()),
        },
      }); 
      app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
    }

    private static bool IsApiRequest(IOwinRequest request)
    {
      string apiPath = VirtualPathUtility.ToAbsolute("~/x-");
      return request.Uri.LocalPath.StartsWith(apiPath);
    }

    private static void RegisterExternalAuth(IAppBuilder app)
    {
//app.UseFacebookAuthentication(
      //   appId: "",
      //   appSecret: "");

      if (!string.IsNullOrWhiteSpace(ApiSecretsStorage.GoogleClientId) &&
          !string.IsNullOrWhiteSpace(ApiSecretsStorage.GoogleClientSecret))
      {
        var googleOAuth2AuthenticationOptions = new GoogleOAuth2AuthenticationOptions()
        {
          ClientId = ApiSecretsStorage.GoogleClientId,
          ClientSecret = ApiSecretsStorage.GoogleClientSecret,
        };
        googleOAuth2AuthenticationOptions.Scope.Add("email");
        app.UseGoogleAuthentication(googleOAuth2AuthenticationOptions);
      }

      if (!string.IsNullOrWhiteSpace(ApiSecretsStorage.VkClientId) &&
          !string.IsNullOrWhiteSpace(ApiSecretsStorage.VkClientSecret))
      {
        app.UseVKontakteAuthentication(new VKontakteAuthenticationOptions()
        {
          Scope = new List<string>() {"email"},
          ClientId = ApiSecretsStorage.VkClientId,
          ClientSecret = ApiSecretsStorage.VkClientSecret,
        });
      }
    }
  }
}
