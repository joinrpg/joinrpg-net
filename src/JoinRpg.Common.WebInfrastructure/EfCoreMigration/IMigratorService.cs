namespace JoinRpg.Common.WebInfrastructure.EfCoreMigration;

public interface IMigratorService
{
    Task MigrateAsync(CancellationToken ct);
}
