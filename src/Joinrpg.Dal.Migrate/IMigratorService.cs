namespace Joinrpg.Dal.Migrate;
internal interface IMigratorService
{
    internal abstract Task MigrateAsync(CancellationToken ct);
}
