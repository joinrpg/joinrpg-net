using System.Data.Entity.Infrastructure;

namespace JoinRpg.Dal.Impl;
public class MyDbContextFactory : IDbContextFactory<MyDbContext>
{
    // DbContextFactory используется только при миграциях, поэтому нормально что тут захардкожено.
    MyDbContext IDbContextFactory<MyDbContext>.Create() => new(new DesignTimeConfig(), logger: null);

    private class DesignTimeConfig : IJoinDbContextConfiguration
    {
        public string ConnectionString => "Data Source=127.0.0.1;User Id=sa;Password=MsSqlPass1!;MultipleActiveResultSets=True;Initial Catalog=joinrpg";
    }
}
