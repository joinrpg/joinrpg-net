using Autofac;
using JoinRpg.Services.Email;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.DI;

public class JoinrpgMainModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        _ = builder.RegisterTypes(Services.Impl.Services.GetTypes().ToArray()).AsImplementedInterfaces().AsSelf();
        _ = builder.RegisterTypes(WebPortal.Managers.Registration.GetTypes().ToArray()).AsSelf().AsImplementedInterfaces();



        _ = builder.RegisterTypes(NotificationRegistration.GetTypes().ToArray()).AsImplementedInterfaces();

        _ = builder.RegisterType<VirtualUsersService>().As<IVirtualUsersService>().SingleInstance();

        _ = builder.RegisterType<PaymentsService>().As<IPaymentsService>();

        base.Load(builder);
    }
}
