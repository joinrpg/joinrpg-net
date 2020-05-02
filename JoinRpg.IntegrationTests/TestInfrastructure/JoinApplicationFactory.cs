using System;
using System.Reflection;
using Autofac;
using JoinRpg.Portal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace JoinRpg.IntegrationTests.TestInfrastructure
{
    public class JoinApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder
                .UseEnvironment("IntegrationTest")
                .ConfigureTestContainer((System.Action<ContainerBuilder>)(containerBuilder =>
                {
                    containerBuilder
                        .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                        .Where(IsStub)
                        .AsSelf()
                        .AsImplementedInterfaces();

                }));

            bool IsStub(Type type)
            {
                return type.FullName?.StartsWith("JoinRpg.IntegrationTests.TestInfrastructure.Stubs") == true;
            }
        }
    }
}
