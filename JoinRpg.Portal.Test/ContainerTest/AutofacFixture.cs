using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using JoinRpg.Dal.Impl;
using JoinRpg.DI;
using JoinRpg.Portal.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace JoinRpg.Portal.Test.ContainerTest
{
    /// <summary>
    /// Allows reuse of Autofac container between tests
    /// </summary>
    public class AutofacFixture : IDisposable
    {
        public readonly IContainer Container;

        public AutofacFixture()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "___"), 
                })
                .Build();
            var startup = new Startup(configuration);

            var builder = new ContainerBuilder();

            var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

            startup.ConfigureServices(serviceCollection);
            builder.Populate(serviceCollection);
            //TODO why RegisterControllersAsServices is not working here?
            builder.RegisterTypes(new ControllerDataSource().GetDataSource().Select(t => t.AsType()).ToArray());
            builder.RegisterTypes(new ViewComponentsDataSource().GetDataSource().Select(t => t.AsType()).ToArray());

            startup.ConfigureContainer(builder);

            builder.RegisterInstance<IConfiguration>(configuration);

            Container = builder.Build();
        }

        public void Dispose()
        {
            Container.Dispose();
        }
    }
}
