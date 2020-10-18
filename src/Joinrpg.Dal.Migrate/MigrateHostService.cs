using System.Data.Entity.Migrations;
using System.Data.Entity.Migrations.Infrastructure;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate
{
    internal class MigrateHostService : OneTimeOperationHostedServiceBase
    {
        private readonly IConfiguration configuration;

        public MigrateHostService(IHostApplicationLifetime applicationLifetime, ILogger<MigrateHostService> logger, IConfiguration configuration)
            : base(applicationLifetime, logger)
        {
            this.configuration = configuration;
        }

        internal override void DoWork()
        {
            logger.LogInformation("Create migration");

            var connectionString = configuration.GetConnectionString("DefaultConnection");

            //TODO mask connection string from logs;
            //logger.LogInformation("Discovered connection string {connectionString}", connectionString);

            var migrator = new MigratorLoggingDecorator(new DbMigrator(new JoinMigrationsConfig(connectionString)), new MigrationsLoggerILoggerAdapter(logger));
            logger.LogInformation("Migrator created");

            logger.LogInformation("Start migration");

            logger.LogInformation("Last local migration {lastLocal}", migrator.GetLocalMigrations().LastOrDefault());
            logger.LogInformation("Last DB migration {lastDb}", migrator.GetDatabaseMigrations().LastOrDefault());

            var pending = migrator.GetPendingMigrations();
            logger.LogInformation("Pending migrations {pending}", string.Join("\n", pending));
            migrator.Update();
            logger.LogInformation("Migration completed");
        }
    }
}
