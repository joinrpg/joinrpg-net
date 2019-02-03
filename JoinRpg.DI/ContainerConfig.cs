using System.Linq;
using JoinRpg.Common.EmailSending.Impl;
using JoinRpg.Dal.Impl;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Impl;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Services.Email;
using JoinRpg.Services.Export;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Email;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.WebPortal.Managers;
using Microsoft.Practices.Unity;

namespace JoinRpg.DI
{
    public class ContainerConfig
    {
        public static void InjectAll(IUnityContainer container)
        {
            container.RegisterTypes(RepositoriesRegistraton.GetTypes(),
                WithMappings.FromAllInterfaces,
                WithName.TypeName);

            container.RegisterTypes(Services.Impl.Services.GetTypes(),
                WithMappings.FromAllInterfaces,
                WithName.TypeName);

            container.RegisterType<IExportDataService, ExportDataServiceImpl>();


            container.RegisterType<IEmailService, EmailServiceImpl>();


            container.RegisterType<IPluginFactory, PluginFactoryImpl>();

            container.RegisterType<IEmailSendingService, EmailSendingServiceImpl>();

            //TODO Automatically load all assemblies that start with JoinRpg.Experimental.Plugin.*
            container.RegisterTypes(
                AllClasses.FromLoadedAssemblies()
                    .Where(type => typeof(IPlugin).IsAssignableFrom(type)),
                WithMappings.FromAllInterfaces,
                WithName.TypeName);

            container.RegisterTypes(Registration.GetTypes(),
                WithMappings.FromAllInterfaces,
                WithName.TypeName);
        }
    }
}
