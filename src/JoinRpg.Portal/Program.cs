using Autofac;
using Autofac.Extensions.DependencyInjection;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Logging;
using Serilog;

namespace JoinRpg.Portal;

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger(); //Will be reconfigured after host initialization

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            var startup = new Startup(builder.Configuration, builder.Environment);

            startup.ConfigureServices(new JoinServiceCollectionProxy(builder.Services));

            builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory());
            builder.Host.ConfigureContainer<ContainerBuilder>(startup.ConfigureContainer);

            builder.Host.UseSerilog((context, _, configuration) =>
             {
                 var loggerOptions = context.Configuration.GetSection("Logging").Get<SerilogOptions>();
                 configuration.ConfigureLogger(loggerOptions!);
             });

            var app = builder.Build();

            startup.Configure(app, app.Environment);

            app.Run();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Host terminated unexpectedly");
            Environment.ExitCode = 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
