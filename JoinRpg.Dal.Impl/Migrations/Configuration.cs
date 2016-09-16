using System.Data.Entity.Migrations;

namespace JoinRpg.Dal.Impl.Migrations
{
  public sealed class Configuration : DbMigrationsConfiguration<MyDbContext>
  {
    public Configuration()
    {
      AutomaticMigrationsEnabled = false;
    }
  }
}