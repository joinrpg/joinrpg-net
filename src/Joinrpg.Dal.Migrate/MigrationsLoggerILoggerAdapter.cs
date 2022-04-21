using System.Data.Entity.Migrations.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Joinrpg.Dal.Migrate;

internal class MigrationsLoggerILoggerAdapter : MigrationsLogger
{
    private readonly ILogger logger;

    public MigrationsLoggerILoggerAdapter(ILogger logger) => this.logger = logger;
    public override void Info(string message) => logger.LogInformation(message);

    public override void Verbose(string message) => logger.LogDebug(message);

    public override void Warning(string message) => logger.LogWarning(message);
}
