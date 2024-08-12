using Autofac;
using JoinRpg.Dal.Impl;
using JoinRpg.Services.Email;
using JoinRpg.Services.Export;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.DI;

public class JoinrpgMainModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterTypes(RepositoriesRegistraton.GetTypes().ToArray()).AsImplementedInterfaces();
        _ = builder.RegisterTypes(Services.Impl.Services.GetTypes().ToArray()).AsImplementedInterfaces().AsSelf();
        _ = builder.RegisterTypes(WebPortal.Managers.Registration.GetTypes().ToArray()).AsSelf().AsImplementedInterfaces();

        _ = builder.RegisterType<ExportDataServiceImpl>().As<IExportDataService>();

        _ = builder.RegisterTypes(NotificationRegistration.GetTypes().ToArray()).AsImplementedInterfaces();

        _ = builder.RegisterType<MyDbContext>()
            .AsSelf()
            .AsImplementedInterfaces()
            .InstancePerDependency()
            .UsingConstructor(typeof(IJoinDbContextConfiguration));

        _ = builder.RegisterType<VirtualUsersService>().As<IVirtualUsersService>().SingleInstance();

        _ = builder.RegisterType<PaymentsService>().As<IPaymentsService>();

        base.Load(builder);
    }
}
