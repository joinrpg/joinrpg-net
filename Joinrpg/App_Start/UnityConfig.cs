using System;
using System.Data.Entity;
using System.Net;
using System.Web;
using JoinRpg.Dal.Impl;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using Microsoft.Practices.Unity;

namespace JoinRpg.Web
{
  /// <summary>
  /// Specifies the Unity configuration for the main container.
  /// </summary>
  public class UnityConfig
  {
    #region Unity Container

    private static Lazy<IUnityContainer> container = new Lazy<IUnityContainer>(() =>
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
      // NOTE: To load from web.config uncomment the line below. Make sure to add a Microsoft.Practices.Unity.Configuration to the using statements.
      // container.LoadConfiguration();

      // TODO: Register your types here
      // container.RegisterType<IProductRepository, ProductRepository>();

      container.RegisterType<IUnitOfWork, MyDbContext>();
      container.RegisterType<DbContext, MyDbContext>();

      container.RegisterType<IProjectRepository, ProjectRepository>();
      container.RegisterType<IUserRepository, UserInfoRepository>();

      container.RegisterType<IProjectService, ProjectService>();
      container.RegisterType<IClaimService, ClaimServiceImpl>();

      container.RegisterType<IUserStore<User, int>, MyUserStore>();

      container.RegisterType<IAuthenticationManager>(
        new InjectionFactory(o => HttpContext.Current.GetOwinContext().Authentication));
    }
  }
}
