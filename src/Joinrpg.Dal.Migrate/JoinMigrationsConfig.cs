using System.Data.Entity.Migrations;
using JoinRpg.Dal.Impl;

namespace Joinrpg.Dal.Migrate;

internal class JoinMigrationsConfig : DbMigrationsConfiguration<MyDbContext>
{
    public JoinMigrationsConfig(string connectionString)
    {
        TargetDatabase = new System.Data.Entity.Infrastructure.DbConnectionInfo(connectionString, "System.Data.SqlClient");
        MigrationsAssembly = typeof(MyDbContext).Assembly;
        MigrationsNamespace = typeof(MyDbContext).Namespace + ".Migrations";
        ContextKey = "JoinRpg.Dal.Impl.Migrations.Configuration";
    }
}
