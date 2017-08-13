namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ResponsibleMaster : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Claims", "ResponsibleMasterUserId", c => c.Int());
            AddColumn("dbo.CharacterGroups", "ResponsibleMasterUserId", c => c.Int());
            CreateIndex("dbo.Claims", "ResponsibleMasterUserId");
            CreateIndex("dbo.CharacterGroups", "ResponsibleMasterUserId");
            AddForeignKey("dbo.CharacterGroups", "ResponsibleMasterUserId", "dbo.Users", "UserId");
            AddForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users", "UserId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Claims", "ResponsibleMasterUserId", "dbo.Users");
            DropForeignKey("dbo.CharacterGroups", "ResponsibleMasterUserId", "dbo.Users");
            DropIndex("dbo.CharacterGroups", new[] { "ResponsibleMasterUserId" });
            DropIndex("dbo.Claims", new[] { "ResponsibleMasterUserId" });
            DropColumn("dbo.CharacterGroups", "ResponsibleMasterUserId");
            DropColumn("dbo.Claims", "ResponsibleMasterUserId");
        }
    }
}
