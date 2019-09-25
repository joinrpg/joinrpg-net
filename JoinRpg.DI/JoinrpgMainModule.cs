using System.Linq;
using Autofac;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.Dal.Impl;
using JoinRpg.PluginHost.Impl;
using JoinRpg.PluginHost.Interfaces;
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
            builder.RegisterTypes(RepositoriesRegistraton.GetTypes().ToArray()).AsImplementedInterfaces();
            builder.RegisterTypes(Services.Impl.Services.GetTypes().ToArray()).AsImplementedInterfaces();
            builder.RegisterTypes(WebPortal.Managers.Registration.GetTypes().ToArray()).AsSelf();

            builder.RegisterType<ExportDataServiceImpl>().As<IExportDataService>();
            builder.RegisterType<EmailServiceImpl>().As<IEmailService>();
            builder.RegisterType<EmailSendingServiceImpl>().As<IEmailSendingService>();

            builder.RegisterType<PluginFactoryImpl>().As<IPluginFactory>();

            builder.RegisterType<MyDbContext>().AsSelf().AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterType<VirtualUsersService>()
                .As<IVirtualUsersService>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
