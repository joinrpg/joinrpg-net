namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class DefaultTemplate : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectDetails", "DefaultTemplateCharacterId", c => c.Int());
        CreateIndex("dbo.ProjectDetails", "DefaultTemplateCharacterId");
        AddForeignKey("dbo.ProjectDetails", "DefaultTemplateCharacterId", "dbo.Characters", "CharacterId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.ProjectDetails", "DefaultTemplateCharacterId", "dbo.Characters");
        DropIndex("dbo.ProjectDetails", new[] { "DefaultTemplateCharacterId" });
        DropColumn("dbo.ProjectDetails", "DefaultTemplateCharacterId");
    }
}
