using Autofac;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Web.Helpers;
using Microsoft.AspNetCore.Authorization;
using CurrentProjectAccessor = JoinRpg.Portal.Infrastructure.CurrentProjectAccessor;

namespace JoinRpg.Portal
{
    internal class JoinRpgPortalModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApplicationUserManager>();
            builder.RegisterType<ApplicationSignInManager>();
            builder.RegisterType<UriServiceImpl>().AsImplementedInterfaces();
            builder.RegisterType<ConfigurationAdapter>().AsSelf().AsImplementedInterfaces();

            builder.RegisterType<Web.Helpers.CurrentUserAccessor>().AsImplementedInterfaces();
            builder.RegisterType<CurrentProjectAccessor>().AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(typeof(JoinRpgPortalModule).Assembly)
                .Where(type => typeof(IAuthorizationHandler).IsAssignableFrom(type)).As<IAuthorizationHandler>();

        }
    }
}
