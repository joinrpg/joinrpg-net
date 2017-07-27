namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ExternalLogins : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserExternalLogins",
                c => new
                    {
                        UserExternalLoginId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Provider = c.String(),
                        Key = c.String(),
                    })
                .PrimaryKey(t => t.UserExternalLoginId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.UserExternalLogins", "UserId", "dbo.Users");
            DropIndex("dbo.UserExternalLogins", new[] { "UserId" });
            DropTable("dbo.UserExternalLogins");
        }
    }
}
