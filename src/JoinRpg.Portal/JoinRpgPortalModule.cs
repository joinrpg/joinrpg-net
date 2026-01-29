using Autofac;
using BitArmory.ReCaptcha;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.WebPortal.Managers.Projects;
using Microsoft.AspNetCore.Authorization;
using PscbApi;
using CurrentProjectAccessor = JoinRpg.Portal.Infrastructure.CurrentProjectAccessor;

namespace JoinRpg.Portal;

internal class JoinRpgPortalModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterTypes(Services.Impl.Services.GetTypes().ToArray()).AsImplementedInterfaces().AsSelf();

        _ = builder.RegisterType<ApplicationSignInManager>();
        _ = builder.RegisterType<UriServiceImpl>().AsImplementedInterfaces();
        _ = builder.RegisterType<ConfigurationAdapter>().AsImplementedInterfaces();

        _ = builder.RegisterType<CurrentProjectAccessor>().AsImplementedInterfaces();

        _ = builder.RegisterType<ReCaptchaService>().SingleInstance();
        _ = builder.RegisterType<RecaptchaVerificator>().AsImplementedInterfaces();

        _ = builder.RegisterType<ExternalLoginProfileExtractor>();

        builder.RegisterDecorator<ProjectMetadataRepositoryCacheDecorator, IProjectMetadataRepository>();

        builder.RegisterAssemblyTypes(typeof(JoinRpgPortalModule).Assembly)
            .Where(type => typeof(IAuthorizationHandler).IsAssignableFrom(type)).As<IAuthorizationHandler>();
    }
}
