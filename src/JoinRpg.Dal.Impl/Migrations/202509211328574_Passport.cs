namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class Passport : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.Claims", "PlayerAllowedSenstiveData", c => c.Boolean(nullable: false));
        AddColumn("dbo.UserExtras", "EnableTelegramPlayerDigestNotification", c => c.Boolean(nullable: false, defaultValue: true));
        AddColumn("dbo.UserExtras", "PassportData", c => c.String());
        AddColumn("dbo.UserExtras", "RegistrationAddress", c => c.String());
        AddColumn("dbo.ProjectDetails", "RequirePassport", c => c.Int(nullable: false));
        AddColumn("dbo.ProjectDetails", "RequireRegistrationAddress", c => c.Int(nullable: false));
    }

    public override void Down()
    {
        DropColumn("dbo.ProjectDetails", "RequireRegistrationAddress");
        DropColumn("dbo.ProjectDetails", "RequirePassport");
        DropColumn("dbo.UserExtras", "RegistrationAddress");
        DropColumn("dbo.UserExtras", "PassportData");
        DropColumn("dbo.UserExtras", "EnableTelegramPlayerDigestNotification");
        DropColumn("dbo.Claims", "PlayerAllowedSenstiveData");
    }
}
