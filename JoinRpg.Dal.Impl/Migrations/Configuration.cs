using System.Data.Entity.Migrations;
using JetBrains.Annotations;

namespace JoinRpg.Dal.Impl.Migrations
{
  [UsedImplicitly]
  internal sealed class Configuration : DbMigrationsConfiguration<MyDbContext>
  {
    public Configuration()
    {
      AutomaticMigrationsEnabled = true;
    }
  }
}