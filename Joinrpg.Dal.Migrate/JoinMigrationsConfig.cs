using System.Data.Entity.Migrations;

namespace Joinrpg.Dal.Migrate
{
    internal class JoinMigrationsConfig : DbMigrationsConfiguration
    {
        public JoinMigrationsConfig(string connectionString)
        {
            TargetDatabase = new System.Data.Entity.Infrastructure.DbConnectionInfo(connectionString, "System.Data.SqlClient");
        }
    }
}
