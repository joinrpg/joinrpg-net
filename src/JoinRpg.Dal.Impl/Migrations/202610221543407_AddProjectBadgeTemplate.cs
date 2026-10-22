namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AddProjectBadgeTemplate : DbMigration
{
    public override void Up()
    {
        CreateTable(
            "dbo.ProjectBadgeTemplates",
            c => new
            {
                ProjectBadgeTemplateId = c.Int(nullable: false, identity: true),
                ProjectId = c.Int(nullable: false),
                Name = c.String(nullable: false, maxLength: 100),
                WidthMm = c.Int(nullable: false),
                HeightMm = c.Int(nullable: false),
                BackgroundUrl = c.String(maxLength: 1024),
                LinesJson = c.String(nullable: false),
                IsActive = c.Boolean(nullable: false),
                CreatedAt = c.DateTime(nullable: false),
                CreatedById = c.Int(nullable: false),
                UpdatedAt = c.DateTime(nullable: false),
                UpdatedById = c.Int(nullable: false),
            })
            .PrimaryKey(t => t.ProjectBadgeTemplateId)
            .ForeignKey("dbo.Users", t => t.CreatedById)
            .ForeignKey("dbo.Projects", t => t.ProjectId, cascadeDelete: true)
            .ForeignKey("dbo.Users", t => t.UpdatedById)
            .Index(t => t.ProjectId)
            .Index(t => t.CreatedById)
            .Index(t => t.UpdatedById);
    }

    public override void Down()
    {
        DropForeignKey("dbo.ProjectBadgeTemplates", "UpdatedById", "dbo.Users");
        DropForeignKey("dbo.ProjectBadgeTemplates", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.ProjectBadgeTemplates", "CreatedById", "dbo.Users");
        DropIndex("dbo.ProjectBadgeTemplates", new[] { "UpdatedById" });
        DropIndex("dbo.ProjectBadgeTemplates", new[] { "CreatedById" });
        DropIndex("dbo.ProjectBadgeTemplates", new[] { "ProjectId" });
        DropTable("dbo.ProjectBadgeTemplates");
    }
}
