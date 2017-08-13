namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class MoreRightsForAcl : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Projects", "CreatorUserId", "dbo.Users");
            DropIndex("dbo.Projects", new[] { "CreatorUserId" });
            AddColumn("dbo.ProjectAcls", "IsOwner", c => c.Boolean(nullable: false));
            AddColumn("dbo.ProjectAcls", "CanGrantRights", c => c.Boolean(nullable: false));
            DropColumn("dbo.Projects", "CreatorUserId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Projects", "CreatorUserId", c => c.Int(nullable: false));
            DropColumn("dbo.ProjectAcls", "CanGrantRights");
            DropColumn("dbo.ProjectAcls", "IsOwner");
            CreateIndex("dbo.Projects", "CreatorUserId");
            AddForeignKey("dbo.Projects", "CreatorUserId", "dbo.Users", "UserId");
        }
    }
}
