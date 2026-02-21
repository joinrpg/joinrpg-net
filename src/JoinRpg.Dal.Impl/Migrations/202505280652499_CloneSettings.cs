namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

public partial class CloneSettings : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.ProjectDetails", "ClonedFromProjectId", c => c.Int());
        AddColumn("dbo.ProjectDetails", "ProjectCloneSettings", c => c.Int(nullable: false, defaultValue: (int)ProjectCloneSettings.CanBeClonedByMaster));
        CreateIndex("dbo.ProjectDetails", "ClonedFromProjectId");
        AddForeignKey("dbo.ProjectDetails", "ClonedFromProjectId", "dbo.Projects", "ProjectId");
    }

    public override void Down()
    {
        DropForeignKey("dbo.ProjectDetails", "ClonedFromProjectId", "dbo.Projects");
        DropIndex("dbo.ProjectDetails", new[] { "ClonedFromProjectId" });
        DropColumn("dbo.ProjectDetails", "ProjectCloneSettings");
        DropColumn("dbo.ProjectDetails", "ClonedFromProjectId");
    }
}
