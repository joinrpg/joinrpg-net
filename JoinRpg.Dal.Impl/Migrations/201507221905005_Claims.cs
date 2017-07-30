namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Claims : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Claims",
                c => new
                    {
                        ClaimId = c.Int(nullable: false, identity: true),
                        CharacterId = c.Int(),
                        CharacterGroupId = c.Int(),
                        PlayerUserId = c.Int(nullable: false),
                        PlayerAcceptedDate = c.DateTime(),
                        PlayerDeclinedDate = c.DateTime(),
                        MasterAcceptedDate = c.DateTime(),
                        MasterDeclinedDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.ClaimId)
                .ForeignKey("dbo.Users", t => t.PlayerUserId)
                .ForeignKey("dbo.Characters", t => t.CharacterId)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroupId)
                .Index(t => t.CharacterId)
                .Index(t => t.CharacterGroupId)
                .Index(t => t.PlayerUserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Claims", "CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.Claims", "CharacterId", "dbo.Characters");
            DropForeignKey("dbo.Claims", "PlayerUserId", "dbo.Users");
            DropIndex("dbo.Claims", new[] { "PlayerUserId" });
            DropIndex("dbo.Claims", new[] { "CharacterGroupId" });
            DropIndex("dbo.Claims", new[] { "CharacterId" });
            DropTable("dbo.Claims");
        }
    }
}
