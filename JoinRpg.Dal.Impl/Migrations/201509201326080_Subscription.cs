namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Subscription : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserSubscriptions",
                c => new
                    {
                        UserSubscriptionId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        ProjectId = c.Int(nullable: false),
                        CharacterGroupId = c.Int(),
                        CharacterId = c.Int(),
                        ClaimId = c.Int(),
                        ClaimStatusChange = c.Boolean(nullable: false),
                        Comments = c.Boolean(nullable: false),
                        FieldChange = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.UserSubscriptionId)
                .ForeignKey("dbo.Characters", t => t.CharacterId)
                .ForeignKey("dbo.CharacterGroups", t => t.CharacterGroupId)
                .ForeignKey("dbo.Claims", t => t.ClaimId)
                .ForeignKey("dbo.Projects", t => t.ProjectId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.ProjectId)
                .Index(t => t.CharacterGroupId)
                .Index(t => t.CharacterId)
                .Index(t => t.ClaimId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserSubscriptions", "UserId", "dbo.Users");
            DropForeignKey("dbo.UserSubscriptions", "ProjectId", "dbo.Projects");
            DropForeignKey("dbo.UserSubscriptions", "ClaimId", "dbo.Claims");
            DropForeignKey("dbo.UserSubscriptions", "CharacterGroupId", "dbo.CharacterGroups");
            DropForeignKey("dbo.UserSubscriptions", "CharacterId", "dbo.Characters");
            DropIndex("dbo.UserSubscriptions", new[] { "ClaimId" });
            DropIndex("dbo.UserSubscriptions", new[] { "CharacterId" });
            DropIndex("dbo.UserSubscriptions", new[] { "CharacterGroupId" });
            DropIndex("dbo.UserSubscriptions", new[] { "ProjectId" });
            DropIndex("dbo.UserSubscriptions", new[] { "UserId" });
            DropTable("dbo.UserSubscriptions");
        }
    }
}
