using System.Data.Entity.Migrations;
using JoinRpg.Dal.Impl;

namespace Joinrpg.Dal.Migrate.Ef6;

internal class JoinMigrationsConfig : DbMigrationsConfiguration<MyDbContext>
{
    public JoinMigrationsConfig(string connectionString)
    {
        TargetDatabase = new System.Data.Entity.Infrastructure.DbConnectionInfo(connectionString, "System.Data.SqlClient");
        MigrationsAssembly = typeof(MyDbContext).Assembly;
        MigrationsNamespace = typeof(MyDbContext).Namespace + ".Migrations";
        ContextKey = "JoinRpg.Dal.Impl.Migrations.Configuration";
        CommandTimeout = 900;  // seconds
    }
}
