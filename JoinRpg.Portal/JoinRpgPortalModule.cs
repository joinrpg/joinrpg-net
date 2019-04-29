using Autofac;
using JoinRpg.Portal.Identity;

namespace JoinRpg.Portal
{
    internal class JoinRpgPortalModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApplicationUserManager>();
            builder.RegisterType<ApplicationSignInManager>();
            builder.RegisterType<Infrastructure.UriServiceImpl>().AsImplementedInterfaces();
            builder.RegisterType<Infrastructure.ConfigurationAdapter>().AsImplementedInterfaces();

            builder.RegisterType<Web.Helpers.CurrentUserAccessor>().AsImplementedInterfaces();

        }
    }
}
