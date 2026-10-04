namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class ProjectTimeZone : DbMigration
{
    public override void Up()
    {
        // Все существующие проекты считаем московскими — так они и жили до появления настройки.
        AddColumn("dbo.ProjectDetails", "TimeZoneId", c => c.String(nullable: false, maxLength: 64, defaultValue: "Europe/Moscow"));
    }

    public override void Down()
    {
        DropColumn("dbo.ProjectDetails", "TimeZoneId");
    }
}
