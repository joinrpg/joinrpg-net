using System.Data.Entity.Migrations;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;
using Autofac.Integration.WebApi;
using Joinrpg.Web.Identity;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using Microsoft.Owin.Security;

namespace JoinRpg.Web.App_Start
{
    public class WebModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<DI.JoinrpgMainModule>();
            builder.RegisterModule<PluginsModule>();

            builder.RegisterType<ApplicationUserManager>().InstancePerLifetimeScope();
            builder.RegisterType<ApplicationSignInManager>().InstancePerLifetimeScope();

            builder.RegisterType<EmailService>().AsImplementedInterfaces();

            builder.RegisterType<ApiSecretsStorage>().AsImplementedInterfaces();

            builder.RegisterType<CurrentUserAccessor>().AsImplementedInterfaces();
            builder.RegisterType<CurrentProjectAccessor>().AsImplementedInterfaces();
            builder.Register(c => HttpContext.Current.GetOwinContext().Authentication).As<IAuthenticationManager>().InstancePerLifetimeScope();
            builder.Register(c => new UriServiceImpl(new HttpContextWrapper(HttpContext.Current))).As<IUriService>().InstancePerLifetimeScope();
            builder.RegisterType<MyUserStore>().AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
