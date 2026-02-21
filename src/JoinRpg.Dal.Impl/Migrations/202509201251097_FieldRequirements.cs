namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class FieldRequirements : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectDetails", "RequireRealName", c => c.Int(nullable: false));
        AddColumn("dbo.ProjectDetails", "RequireTelegram", c => c.Int(nullable: false));
        AddColumn("dbo.ProjectDetails", "RequireVkontakte", c => c.Int(nullable: false));
        AddColumn("dbo.ProjectDetails", "RequirePhone", c => c.Int(nullable: false));
    }

    public override void Down()
    {
        DropColumn("dbo.ProjectDetails", "RequirePhone");
        DropColumn("dbo.ProjectDetails", "RequireVkontakte");
        DropColumn("dbo.ProjectDetails", "RequireTelegram");
        DropColumn("dbo.ProjectDetails", "RequireRealName");
    }
}
