namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class HotSetka : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectRolesLists", "ShowRolesFilter", c => c.Int(nullable: false, defaultValue: 0));
    }

    public override void Down()
    {
        DropColumn("dbo.ProjectRolesLists", "ShowRolesFilter");
    }
}
