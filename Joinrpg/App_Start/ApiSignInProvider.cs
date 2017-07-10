using System.Data.Entity;
using System.Threading.Tasks;
using JoinRpg.Web.Helpers;
using Microsoft.Owin.Security.OAuth;

namespace JoinRpg.Web
{
  internal class ApiSignInProvider : OAuthAuthorizationServerProvider
  {
    private ApplicationUserManager Manager { get; }

    public ApiSignInProvider(ApplicationUserManager manager)
    {
      Manager = manager;
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

      var user = await Manager.FindByEmailAsync(context.UserName);

      if (await Manager.CheckPasswordAsync(user, context.Password))
      {
        context.SetError("invalid_grant", "The user name or password is incorrect.");
        return;
      }

      var x = await user.GenerateUserIdentityAsync(Manager, context.Options.AuthenticationType);

      context.Validated(x);

    }
  }
}