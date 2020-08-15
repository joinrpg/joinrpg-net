using System;
using System.Reflection;
using Autofac;
using JoinRpg.Dal.Impl;
using JoinRpg.Portal;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.IntegrationTests.TestInfrastructure
{
    public class JoinApplicationFactory : WebApplicationFactory<Startup>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            var builder = base.CreateHostBuilder();
            return builder
                .UseEnvironment("IntegrationTest")
                .ConfigureContainer<ContainerBuilder>(containerBuilder =>
                {
                    containerBuilder
                        .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                        .Where(IsStub)
                        .AsSelf()
                        .AsImplementedInterfaces();

                });

            bool IsStub(Type type)
            {
                return type.FullName?.StartsWith("JoinRpg.IntegrationTests.TestInfrastructure.Stubs") == true;
            }
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseTestServer();
        }

        protected override void Dispose(bool disposing)
        {
            var context = Services.GetRequiredService<MyDbContext>();
            context.Database.Delete();
            base.Dispose(disposing);
        }
    }
}
