namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class PluginFieldMapping : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ProjectPlugins", "Project_ProjectId", "dbo.Projects");
            DropIndex("dbo.ProjectPlugins", new[] { "Project_ProjectId" });
            RenameColumn(table: "dbo.ProjectPlugins", name: "Project_ProjectId", newName: "ProjectId");
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
                .PrimaryKey(t => t.PluginFieldMappingId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .ForeignKey("dbo.ProjectFields", t => t.ProjectFieldId)
                .ForeignKey("dbo.ProjectPlugins", t => t.ProjectPluginId)
                .Index(t => t.ProjectId)
                .Index(t => t.ProjectFieldId)
                .Index(t => t.ProjectPluginId);
            
            AlterColumn("dbo.ProjectPlugins", "ProjectId", c => c.Int(nullable: false));
            CreateIndex("dbo.ProjectPlugins", "ProjectId");
            AddForeignKey("dbo.ProjectPlugins", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: true);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectPlugins", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.PluginFieldMappings", "ProjectPluginId", "dbo.ProjectPlugins");
            DropForeignKey("dbo.PluginFieldMappings", "ProjectFieldId", "dbo.ProjectFields");
            DropForeignKey("dbo.PluginFieldMappings", "ProjectId", "dbo.Projects");
            DropIndex("dbo.PluginFieldMappings", new[] { "ProjectPluginId" });
            DropIndex("dbo.PluginFieldMappings", new[] { "ProjectFieldId" });
            DropIndex("dbo.PluginFieldMappings", new[] { "ProjectId" });
            DropIndex("dbo.ProjectPlugins", new[] { "ProjectId" });
            AlterColumn("dbo.ProjectPlugins", "ProjectId", c => c.Int());
            DropTable("dbo.PluginFieldMappings");
            RenameColumn(table: "dbo.ProjectPlugins", name: "ProjectId", newName: "Project_ProjectId");
            CreateIndex("dbo.ProjectPlugins", "Project_ProjectId");
            AddForeignKey("dbo.ProjectPlugins", "Project_ProjectId", "dbo.Projects", "ProjectId");
        }
    }
}
