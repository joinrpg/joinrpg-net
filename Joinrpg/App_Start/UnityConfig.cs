using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Impl;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Services.Email;
using JoinRpg.Services.Export;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Microsoft.Practices.Unity;

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

      RepositoriesRegistraton.Register(container);

      Services.Impl.Services.Register(container);

      container.RegisterType<IExportDataService, ExportDataServiceImpl>();

      container.RegisterType<IUriService>(new InjectionFactory(c => new UriServiceImpl(new HttpContextWrapper(HttpContext.Current))));

      container.RegisterType<IEmailService, EmailServiceImpl>();

      container.RegisterType<IMailGunConfig, ApiSecretsStorage>();

      container.RegisterType<IUserStore<User, int>, MyUserStore>();

      container.RegisterType<IPluginFactory, PluginFactoryImpl>();

      //TODO Automatically load all assemblies that start with JoinRpg.Experimental.Plugin.*
      container.RegisterTypes(AllClasses.FromLoadedAssemblies().Where(type => typeof(IPlugin).IsAssignableFrom(type)),
        WithMappings.FromAllInterfaces, WithName.TypeName);
    }
  }
}
