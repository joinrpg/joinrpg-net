using System.Data.Entity.Migrations;

namespace JoinRpg.Dal.Impl.Migrations;

public partial class RemovePlugins : DbMigration
{
    public override void Up()
    {
        Sql(@"
UPDATE [dbo].[ProjectFields]
	SET FieldType = 10
	
	FROM [dbo].[ProjectFields] PF
  INNER JOIN [dbo].[PluginFieldMappings] PFM ON PF.ProjectFieldId = PFM.ProjectFieldId
  WHERE MappingName = 'GeneratePinOperation' AND FieldType IN (0,6)
");

        DropForeignKey("dbo.PluginFieldMappings", "ProjectId", "dbo.Projects");
        DropForeignKey("dbo.PluginFieldMappings", "ProjectFieldId", "dbo.ProjectFields");
        DropForeignKey("dbo.PluginFieldMappings", "ProjectPluginId", "dbo.ProjectPlugins");
        DropForeignKey("dbo.ProjectPlugins", "ProjectId", "dbo.Projects");
        DropIndex("dbo.PluginFieldMappings", new[] { "ProjectId" });
        DropIndex("dbo.PluginFieldMappings", new[] { "ProjectFieldId" });
        DropIndex("dbo.PluginFieldMappings", new[] { "ProjectPluginId" });
        DropIndex("dbo.ProjectPlugins", new[] { "ProjectId" });
        DropTable("dbo.PluginFieldMappings");
        DropTable("dbo.ProjectPlugins");
    }

    public override void Down()
    {
        CreateTable(
            "dbo.ProjectPlugins",
            c => new
            {
                ProjectPluginId = c.Int(nullable: false, identity: true),
                ProjectId = c.Int(nullable: false),
                Name = c.String(),
                Configuration = c.String(),
            })
            .PrimaryKey(t => t.ProjectPluginId);

        CreateTable(
            "dbo.PluginFieldMappings",
            c => new
            {
                PluginFieldMappingId = c.Int(nullable: false, identity: true),
                ProjectId = c.Int(nullable: false),
                ProjectFieldId = c.Int(nullable: false),
                ProjectPluginId = c.Int(nullable: false),
                MappingName = c.String(nullable: false),
                PluginFieldMappingType = c.Int(nullable: false),
            })
            .PrimaryKey(t => t.PluginFieldMappingId);

        CreateIndex("dbo.ProjectPlugins", "ProjectId");
        CreateIndex("dbo.PluginFieldMappings", "ProjectPluginId");
        CreateIndex("dbo.PluginFieldMappings", "ProjectFieldId");
        CreateIndex("dbo.PluginFieldMappings", "ProjectId");
        AddForeignKey("dbo.ProjectPlugins", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
        AddForeignKey("dbo.PluginFieldMappings", "ProjectPluginId", "dbo.ProjectPlugins", "ProjectPluginId", cascadeDelete: true);
        AddForeignKey("dbo.PluginFieldMappings", "ProjectFieldId", "dbo.ProjectFields", "ProjectFieldId", cascadeDelete: true);
        AddForeignKey("dbo.PluginFieldMappings", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
    }
}
