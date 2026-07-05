namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class SetkaShowCharacterGroups : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectRolesLists", "ShowCharacterGroups", c => c.Boolean(nullable: false, defaultValue: true));
    }

    public override void Down()
    {
        DropColumn("dbo.ProjectRolesLists", "ShowCharacterGroups");
    }
}
