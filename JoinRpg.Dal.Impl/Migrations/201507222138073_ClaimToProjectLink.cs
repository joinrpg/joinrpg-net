namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ClaimToProjectLink : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Claims", "ProjectId", c => c.Int(nullable: false));
            CreateIndex("dbo.Claims", "ProjectId");
            AddForeignKey("dbo.Claims", "ProjectId", "dbo.Projects", "ProjectId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Claims", "ProjectId", "dbo.Projects");
            DropIndex("dbo.Claims", new[] { "ProjectId" });
            DropColumn("dbo.Claims", "ProjectId");
        }
    }
}
