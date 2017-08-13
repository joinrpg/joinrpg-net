namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class PlotTags : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ProjectItemTags",
                c => new
                    {
                        ProjectItemTagId = c.Int(nullable: false, identity: true),
                        TagName = c.String(maxLength: 400),
                        PlotFolder_PlotFolderId = c.Int(),
                    })
                .PrimaryKey(t => t.ProjectItemTagId)
                .ForeignKey("dbo.PlotFolders", t => t.PlotFolder_PlotFolderId)
                .Index(t => t.TagName, unique: true)
                .Index(t => t.PlotFolder_PlotFolderId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProjectItemTags", "PlotFolder_PlotFolderId", "dbo.PlotFolders");
            DropIndex("dbo.ProjectItemTags", new[] { "PlotFolder_PlotFolderId" });
            DropIndex("dbo.ProjectItemTags", new[] { "TagName" });
            DropTable("dbo.ProjectItemTags");
        }
    }
}
