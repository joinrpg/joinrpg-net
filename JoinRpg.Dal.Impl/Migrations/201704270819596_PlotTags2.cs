namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class PlotTags2 : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ProjectItemTags", "PlotFolder_PlotFolderId", "dbo.PlotFolders");
            DropIndex("dbo.ProjectItemTags", new[] { "PlotFolder_PlotFolderId" });
            CreateTable(
                "dbo.PlotFolderProjectItemTags",
                c => new
                    {
                        PlotFolder_PlotFolderId = c.Int(nullable: false),
                        ProjectItemTag_ProjectItemTagId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.PlotFolder_PlotFolderId, t.ProjectItemTag_ProjectItemTagId })
                .ForeignKey("dbo.PlotFolders", t => t.PlotFolder_PlotFolderId, cascadeDelete: true)
                .ForeignKey("dbo.ProjectItemTags", t => t.ProjectItemTag_ProjectItemTagId, cascadeDelete: true)
                .Index(t => t.PlotFolder_PlotFolderId)
                .Index(t => t.ProjectItemTag_ProjectItemTagId);

            Sql("DELETE FROM dbo.ProjectItemTags");
            
            DropColumn("dbo.ProjectItemTags", "PlotFolder_PlotFolderId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ProjectItemTags", "PlotFolder_PlotFolderId", c => c.Int());
            DropForeignKey("dbo.PlotFolderProjectItemTags", "ProjectItemTag_ProjectItemTagId", "dbo.ProjectItemTags");
            DropForeignKey("dbo.PlotFolderProjectItemTags", "PlotFolder_PlotFolderId", "dbo.PlotFolders");
            DropIndex("dbo.PlotFolderProjectItemTags", new[] { "ProjectItemTag_ProjectItemTagId" });
            DropIndex("dbo.PlotFolderProjectItemTags", new[] { "PlotFolder_PlotFolderId" });
            DropTable("dbo.PlotFolderProjectItemTags");
            CreateIndex("dbo.ProjectItemTags", "PlotFolder_PlotFolderId");
            AddForeignKey("dbo.ProjectItemTags", "PlotFolder_PlotFolderId", "dbo.PlotFolders", "PlotFolderId");
        }
    }
}
