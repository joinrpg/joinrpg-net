namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class ClaimResponsibleMasterNoCascade : DbMigration
{
    public override void Up()
    {
        DropForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users");
        AddForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users", "UserId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users");
        AddForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users", "UserId", cascadeDelete: true);
    }
}
