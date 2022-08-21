using Autofac.Extensions.DependencyInjection;
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
            CreateHostBuilder(args).Build().Run();
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

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host
            .CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .UseSerilog((context, _, configuration) =>
            {
                var loggerOptions = context.Configuration.GetSection("Logging").Get<SerilogOptions>();
                configuration.ConfigureLogger(loggerOptions);
            })
            .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>());
}
