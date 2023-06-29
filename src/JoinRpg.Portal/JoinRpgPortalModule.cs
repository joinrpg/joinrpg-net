using Autofac;
using BitArmory.ReCaptcha;
using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.Authentication.Avatars;
using JoinRpg.Web.Helpers;
using JoinRpg.WebPortal.Managers;
using Microsoft.AspNetCore.Authorization;
using CurrentProjectAccessor = JoinRpg.Portal.Infrastructure.CurrentProjectAccessor;

namespace JoinRpg.Portal;

internal class JoinRpgPortalModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterType<ApplicationUserManager>();
        _ = builder.RegisterType<ApplicationSignInManager>();
        _ = builder.RegisterType<UriServiceImpl>().AsImplementedInterfaces();
        _ = builder.RegisterType<ConfigurationAdapter>().AsSelf().AsImplementedInterfaces();

        _ = builder.RegisterType<CurrentUserAccessor>().AsImplementedInterfaces();
        _ = builder.RegisterType<CurrentProjectAccessor>().AsImplementedInterfaces();

        _ = builder.RegisterType<ReCaptchaService>().SingleInstance();
        _ = builder.RegisterType<RecaptchaVerificator>().AsImplementedInterfaces();

        _ = builder.RegisterType<ExternalLoginProfileExtractor>();

        builder.RegisterType<AvatarLoader>().AsImplementedInterfaces();
        builder.RegisterDecorator<AvatarCacheDecoractor, IAvatarLoader>();
        builder.RegisterType<AvatarCacheDecoractor>().AsSelf();

        builder.RegisterGeneric(typeof(PerRequestCache<,>)).AsSelf().InstancePerLifetimeScope();
        builder.RegisterGeneric(typeof(SingletonCache<,>)).AsSelf().SingleInstance();

        builder.RegisterDecorator<ProjectMetadataRepositoryCacheDecorator, IProjectMetadataRepository>();

        builder.RegisterAssemblyTypes(typeof(JoinRpgPortalModule).Assembly)
            .Where(type => typeof(IAuthorizationHandler).IsAssignableFrom(type)).As<IAuthorizationHandler>();
    }
}
