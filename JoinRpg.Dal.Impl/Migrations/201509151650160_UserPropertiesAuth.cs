namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class UserPropertiesAuth : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.UserAuthDetails",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        LegacyAllRpgInp = c.Int(),
                        EmailConfirmed = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            DropColumn("dbo.Users", "LegacyAllRpgInp");
        }
        
        public override void Down()
        {
            AddColumn("dbo.Users", "LegacyAllRpgInp", c => c.Int());
            DropForeignKey("dbo.UserAuthDetails", "UserId", "dbo.Users");
            DropIndex("dbo.UserAuthDetails", new[] { "UserId" });
            DropTable("dbo.UserAuthDetails");
        }
    }
}
