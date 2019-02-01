using System;
using System.Data.Entity;
using System.Web;
using Joinrpg.Web.Identity;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.DI;
using Microsoft.Practices.Unity;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace JoinRpg.Web
{
    /// <summary>
    /// Specifies the Unity configuration for the main container.
    /// </summary>
    public static class UnityConfig
  {
    #region Unity Container

    private static readonly Lazy<IUnityContainer> container = new Lazy<IUnityContainer>(() =>
    {
      var container = new UnityContainer();
      RegisterTypes(container);
      return container;
    });

    /// <summary>
    /// Gets the configured Unity container.
    /// </summary>
    public static IUnityContainer GetConfiguredContainer()
    {
      return container.Value;
    }

    #endregion

    /// <summary>Registers the type mappings with the Unity container.</summary>
    /// <param name="container">The unity container to configure.</param>
    /// <remarks>There is no need to register concrete types such as controllers or API controllers (unless you want to 
    /// change the defaults), as Unity allows resolving a concrete type even if it was not previously registered.</remarks>
    public static void RegisterTypes(IUnityContainer container)
    {

      container.RegisterType<IUnitOfWork, MyDbContext>(new PerRequestLifetimeManager());
      container.RegisterType<DbContext, MyDbContext>(new PerRequestLifetimeManager());

      container.RegisterType<ApplicationUserManager>(new PerRequestLifetimeManager());
      container.RegisterType<IAuthenticationManager>(
        new InjectionFactory(c => HttpContext.Current.GetOwinContext().Authentication));
      container.RegisterType<ApplicationSignInManager>(new PerRequestLifetimeManager());

      container.RegisterType<IIdentityMessageService, EmailService>();

        container.RegisterType<IUriService>(new InjectionFactory(c => new UriServiceImpl(new HttpContextWrapper(HttpContext.Current))));

        container.RegisterType<IUserStore<JoinIdentityUser, int>, MyUserStore>();

        container.RegisterType<IMailGunConfig, ApiSecretsStorage>();

            ContainerConfig.InjectAll(container);
    }
  }
}
