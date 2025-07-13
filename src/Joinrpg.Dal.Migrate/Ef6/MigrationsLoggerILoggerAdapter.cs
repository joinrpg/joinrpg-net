using System.Data.Entity.Migrations.Infrastructure;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Dal.Migrate.Ef6;

internal class MigrationsLoggerILoggerAdapter(ILogger logger) : MigrationsLogger
{
    public override void Info(string message) => logger.LogInformation("EF6 {message}", message);

    public override void Verbose(string message) => logger.LogDebug("EF6 {message}", message);

    public override void Warning(string message) => logger.LogWarning("EF6 {message}", message);
}
