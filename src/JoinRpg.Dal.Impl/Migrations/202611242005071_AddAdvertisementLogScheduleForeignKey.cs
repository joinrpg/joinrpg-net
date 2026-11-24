using System.Data.Entity.Migrations;

namespace JoinRpg.Dal.Impl.Migrations;

public partial class AddAdvertisementLogScheduleForeignKey : DbMigration
{
    public override void Up()
    {
        AddForeignKey("dbo.AdvertisementLogEntries", "ScheduleId", "dbo.AdvertisementSchedules", "AdvertisementScheduleId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.AdvertisementLogEntries", "ScheduleId", "dbo.AdvertisementSchedules");
    }
}
