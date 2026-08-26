namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AddAdvertisementLog : DbMigration
{
    public override void Up()
    {
        CreateTable(
            "dbo.AdvertisementLogEntries",
            c => new
            {
                AdvertisementLogEntryId = c.Int(nullable: false, identity: true),
                ScheduleId = c.Int(nullable: false),
                Method = c.Int(nullable: false),
                ProjectId = c.Int(nullable: false),
                CharacterId = c.Int(),
                Status = c.Int(nullable: false),
                SentAt = c.DateTimeOffset(nullable: false, precision: 7),
            })
            .PrimaryKey(t => t.AdvertisementLogEntryId)
            .ForeignKey("dbo.Characters", t => t.CharacterId)
            .ForeignKey("dbo.Projects", t => t.ProjectId)
            .Index(t => t.ScheduleId)
            .Index(t => t.ProjectId)
            .Index(t => t.CharacterId);

    }

    public override void Down()
    {
        DropForeignKey("dbo.AdvertisementLogEntries", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.AdvertisementLogEntries", "CharacterId", "dbo.Characters");
        DropIndex("dbo.AdvertisementLogEntries", new[] { "CharacterId" });
        DropIndex("dbo.AdvertisementLogEntries", new[] { "ProjectId" });
        DropIndex("dbo.AdvertisementLogEntries", new[] { "ScheduleId" });
        DropTable("dbo.AdvertisementLogEntries");
    }
}
