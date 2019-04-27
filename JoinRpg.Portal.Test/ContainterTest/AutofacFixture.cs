using System;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using JoinRpg.DI;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Test.ContainterTest
{
    /// <summary>
    /// Allows reuse of Autofac containter between tests
    /// </summary>
    public class AutofacFixture : IDisposable
    {
        public readonly IContainer Container;

        public AutofacFixture()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new JoinrpgMainModule());
            builder.RegisterModule(new JoinRpgPortalModule());

            var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            var startup = new Startup(null);

            startup.ConfigureServices(serviceCollection);

            builder.Populate(serviceCollection);

            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(Startup))).Where(type => typeof(Controller).IsAssignableFrom(type));

            Container = builder.Build();
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}
