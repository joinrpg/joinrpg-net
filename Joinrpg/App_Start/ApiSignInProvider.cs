using System;
using System.Threading.Tasks;
using JoinRpg.Web.Helpers;
using Microsoft.Owin.Security.OAuth;

namespace JoinRpg.Web
{
  internal class ApiSignInProvider : OAuthAuthorizationServerProvider
  {
    private Func<ApplicationUserManager> ManagerFactory { get; }

    public ApiSignInProvider(Func<ApplicationUserManager> managerFactory)
    {
      ManagerFactory = managerFactory;
    }
    /// <inheritdoc />
    public override Task ValidateClientAuthentication(
      OAuthValidateClientAuthenticationContext context)
    {
      context.Validated();
      return Task.FromResult(0);
    }

    public override async Task GrantResourceOwnerCredentials(
      OAuthGrantResourceOwnerCredentialsContext context)
    {
      context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] {"*"});

      if (string.IsNullOrWhiteSpace(context.UserName) ||
          string.IsNullOrWhiteSpace(context.Password))
      {
        context.SetError("invalid_grant", "Please supply susername and password.");
        return;
      }

      var manager = ManagerFactory();

      var user = await manager.FindByEmailAsync(context.UserName);

      if (!await manager.CheckPasswordAsync(user, context.Password))
      {
        context.SetError("invalid_grant", "The user name or password is incorrect.");
        return;
      }

      var x = await user.GenerateUserIdentityAsync(manager, context.Options.AuthenticationType);

      context.Validated(x);

    }
  }
}