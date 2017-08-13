namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ProjectPlugins : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectPlugins",
                c => new
                    {
                        ProjectPluginId = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Configuration = c.String(),
                        Project_ProjectId = c.Int(),
                    })
                .PrimaryKey(t => t.ProjectPluginId)
                .ForeignKey("dbo.Projects", t => t.Project_ProjectId)
                .Index(t => t.Project_ProjectId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectPlugins", "Project_ProjectId", "dbo.Projects");
            DropIndex("dbo.ProjectPlugins", new[] { "Project_ProjectId" });
            DropTable("dbo.ProjectPlugins");
        }
    }
}
