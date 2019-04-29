using System.Web;
using Autofac;
using Joinrpg.Web.Identity;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using Microsoft.Owin.Security;
using PscbApi;

namespace JoinRpg.Web.App_Start
{
    public class WebModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterModule<DI.JoinrpgMainModule>();
            builder.RegisterModule<PluginsModule>();

            builder.RegisterType<ApplicationUserManager>().InstancePerLifetimeScope();
            builder.RegisterType<ApplicationSignInManager>().InstancePerLifetimeScope();
            builder.RegisterType<UserTokenProviderFactory>()
                .AsImplementedInterfaces()
                .InstancePerLifetimeScope();

            builder.RegisterType<EmailService>().AsImplementedInterfaces();

            builder.RegisterType<ApiSecretsStorage>().AsImplementedInterfaces();

            builder.RegisterType<BankSecretsProvider>()
                .As<IBankSecretsProvider>()
                .SingleInstance();

            builder.RegisterType<CurrentUserAccessor>().AsImplementedInterfaces();
            builder.RegisterType<CurrentProjectAccessor>().AsImplementedInterfaces();
            builder.Register(c => HttpContext.Current.GetOwinContext().Authentication).As<IAuthenticationManager>().InstancePerLifetimeScope();
            builder.Register(c => new UriServiceImpl(new HttpContextWrapper(HttpContext.Current))).As<IUriService>().InstancePerLifetimeScope();
            builder.RegisterType<MyUserStore>().AsImplementedInterfaces();

            base.Load(builder);
        }
    }
}
