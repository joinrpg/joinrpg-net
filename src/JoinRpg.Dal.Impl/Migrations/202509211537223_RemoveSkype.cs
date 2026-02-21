namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class RemoveSkype : DbMigration
{
    public override void Up()
    {
        DropColumn("dbo.UserExtras", "Skype");
    }

    public override void Down()
    {
        AddColumn("dbo.UserExtras", "Skype", c => c.String());
    }
}
