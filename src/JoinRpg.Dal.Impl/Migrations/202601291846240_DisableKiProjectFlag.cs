namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class DisableKiProjectFlag : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectDetails", "DisableKogdaIgraMapping", c => c.Boolean(nullable: false));
    }

    public override void Down()
    {
        DropColumn("dbo.ProjectDetails", "DisableKogdaIgraMapping");
    }
}
