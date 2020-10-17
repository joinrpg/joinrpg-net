using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate
{
    internal class MigrateHostService : IHostedService
    {
        private CancellationTokenSource CancellationTokenSource { get; } = new CancellationTokenSource();

        private MigratorBase migrator;
        private Task task;
        private readonly IHostApplicationLifetime applicationLifetime;
        private readonly IConfiguration configuration;
        private readonly ILogger<MigrateHostService> logger;

        public MigrateHostService(IHostApplicationLifetime applicationLifetime, IConfiguration configuration, ILogger<MigrateHostService> logger)
        {
            this.applicationLifetime = applicationLifetime;
            this.configuration = configuration;
            this.logger = logger;
        }

        Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Prepare to start MigrateHostService");

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            //TODO mask connection string from logs;
            logger.LogInformation("Discovered connection string {connectionString}", connectionString);

            migrator = new MigratorLoggingDecorator(new DbMigrator(new JoinMigrationsConfig(connectionString)), new MigrationsLoggerILoggerAdapter(logger));

            logger.LogInformation("Migrator created");

            task = Task.Run(async () =>
            {
                await DoWork(CancellationTokenSource.Token);
                applicationLifetime.StopApplication();
            });
            return Task.CompletedTask;
        }
        Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            CancellationTokenSource.Cancel();
            // Defer completion promise, until our application has reported it is done.
            return task;
        }

        private async Task DoWork(CancellationToken cancellationToken)
        {
            logger.LogInformation("Start migration");
            try
            {
                migrator.Update();
            }
            catch (System.Exception exc)
            {
                logger.LogError("Problem during migration", exc);
                throw;
            }

            logger.LogInformation("Migration completed");
        }
    }
}
