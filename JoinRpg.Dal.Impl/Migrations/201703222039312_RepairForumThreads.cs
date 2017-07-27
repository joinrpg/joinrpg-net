namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class RepairForumThreads : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.ForumThreads", "Project_ProjectId", "dbo.Projects");
            DropIndex("dbo.ForumThreads", new[] { "ProjectId" });
            DropIndex("dbo.ForumThreads", new[] { "Project_ProjectId" });
            DropColumn("dbo.ForumThreads", "Project_ProjectId");
            AlterColumn("dbo.ForumThreads", "ProjectId", c => c.Int(nullable: false));
            CreateIndex("dbo.ForumThreads", "ProjectId");
            AddForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects", "ProjectId", cascadeDelete: false);
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects");
            DropIndex("dbo.ForumThreads", new[] { "ProjectId" });
            AlterColumn("dbo.ForumThreads", "ProjectId", c => c.Int());
            AddColumn("dbo.ForumThreads", "Project_ProjectId", c => c.Int());
            CreateIndex("dbo.ForumThreads", "Project_ProjectId");
            CreateIndex("dbo.ForumThreads", "ProjectId");
            AddForeignKey("dbo.ForumThreads", "Project_ProjectId", "dbo.Projects", "ProjectId");
            AddForeignKey("dbo.ForumThreads", "ProjectId", "dbo.Projects", "ProjectId");
        }
    }
}
