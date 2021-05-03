using System.Linq;
using Autofac;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.Dal.Impl;
using JoinRpg.Services.Email;
using JoinRpg.Services.Export;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.DI
{
    public class JoinrpgMainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            _ = builder.RegisterTypes(RepositoriesRegistraton.GetTypes().ToArray()).AsImplementedInterfaces();
            _ = builder.RegisterTypes(Services.Impl.Services.GetTypes().ToArray()).AsImplementedInterfaces();
            _ = builder.RegisterTypes(WebPortal.Managers.Registration.GetTypes().ToArray()).AsSelf();

            _ = builder.RegisterType<ExportDataServiceImpl>().As<IExportDataService>();
            _ = builder.RegisterType<EmailServiceImpl>().As<IEmailService>();
            _ = builder.RegisterType<EmailSendingServiceImpl>().As<IEmailSendingService>();

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
}
