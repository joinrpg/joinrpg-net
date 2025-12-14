using Autofac;
using Autofac.Extensions.DependencyInjection;
using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Portal.Infrastructure;
using Serilog;

namespace JoinRpg.Portal;

public class Program
{
    public static void Main(string[] args)
    {
        using var log = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            var startup = new Startup(builder.Configuration, builder.Environment);

            startup.ConfigureServices(new JoinServiceCollectionProxy(builder.Services));

            _ = builder.Host
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(startup.ConfigureContainer)
                .UseJoinSerilog("JoinRpg.Portal");

            var app = builder.Build();

            startup.Configure(app, app.Environment);

            app.Run();
        }
        catch (Exception e)
        {
            log.Fatal(e, "Host terminated unexpectedly");
            Environment.ExitCode = 1;
        }
    }
}
