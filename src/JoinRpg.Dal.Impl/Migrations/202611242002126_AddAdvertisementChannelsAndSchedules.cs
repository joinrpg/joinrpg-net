using System.Data.Entity.Migrations;

namespace JoinRpg.Dal.Impl.Migrations;

public partial class AddAdvertisementChannelsAndSchedules : DbMigration
{
    public override void Up()
    {
        CreateTable(
            "dbo.AdvertisementChannels",
            c => new
            {
                AdvertisementChannelId = c.Int(nullable: false, identity: true),
                Name = c.String(nullable: false, maxLength: 256),
                ProjectId = c.Int(),
                Kind = c.Int(nullable: false),
                SettingsJson = c.String(nullable: false),
                IsActive = c.Boolean(nullable: false),
            })
            .PrimaryKey(t => t.AdvertisementChannelId)
            .ForeignKey("dbo.Projects", t => t.ProjectId)
            .Index(t => t.ProjectId);

        CreateTable(
            "dbo.AdvertisementSchedules",
            c => new
            {
                AdvertisementScheduleId = c.Int(nullable: false, identity: true),
                AdvertisementChannelId = c.Int(nullable: false),
                Method = c.Int(nullable: false),
                DaysOfWeekMask = c.Int(nullable: false),
                RegionsFilter = c.String(),
                IsActive = c.Boolean(nullable: false),
            })
            .PrimaryKey(t => t.AdvertisementScheduleId)
            .ForeignKey("dbo.AdvertisementChannels", t => t.AdvertisementChannelId)
            .Index(t => t.AdvertisementChannelId);

        // Бэкфилл существующих каналов и расписаний (замена HardcodedAdvertisement*Repository, ADR010).
        // Id сохраняем: на расписания 1-4 ссылается AdvertisementLogEntries (без FK).
        // Маски дней: 127 = каждый день, 8 = среда, 2 = понедельник.
        // Method: 1 = SingleHotRole, 2 = NewlyOpenedProjectsDigest.
        Sql(@"
SET IDENTITY_INSERT AdvertisementChannels ON;
INSERT INTO AdvertisementChannels (AdvertisementChannelId, Name, ProjectId, Kind, SettingsJson, IsActive) VALUES
 (1, N'test_zovem_na_igru', NULL, 1, N'{""ChatId"":""TelegramChatId(-1004315256401)""}', 1),
 (2, N't.me/zovem_joinrpg', NULL, 1, N'{""ChatId"":""TelegramChatId(-1002544815071)""}', 1);
SET IDENTITY_INSERT AdvertisementChannels OFF;
SET IDENTITY_INSERT AdvertisementSchedules ON;
INSERT INTO AdvertisementSchedules (AdvertisementScheduleId, AdvertisementChannelId, Method, DaysOfWeekMask, RegionsFilter, IsActive) VALUES
 (1, 1, 1, 127, NULL, 0),
 (2, 2, 1, 8,   NULL, 1),
 (3, 1, 2, 2,   NULL, 0),
 (4, 2, 2, 2,   NULL, 1);
SET IDENTITY_INSERT AdvertisementSchedules OFF;
");
    }

    public override void Down()
    {
        DropForeignKey("dbo.AdvertisementSchedules", "AdvertisementChannelId", "dbo.AdvertisementChannels");
        DropForeignKey("dbo.AdvertisementChannels", "ProjectId", "dbo.Projects");
        DropIndex("dbo.AdvertisementSchedules", new[] { "AdvertisementChannelId" });
        DropIndex("dbo.AdvertisementChannels", new[] { "ProjectId" });
        DropTable("dbo.AdvertisementSchedules");
        DropTable("dbo.AdvertisementChannels");
    }
}
