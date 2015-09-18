namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AllrpgUserProfiles : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.AllrpgUserDetails",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        Sid = c.Int(),
                        JsonProfile = c.String(),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.UserExtras",
                c => new
                    {
                        UserId = c.Int(nullable: false),
                        GenderByte = c.Byte(nullable: false),
                        Gender = c.Byte(nullable: false),
                        PhoneNumber = c.String(),
                        Skype = c.String(),
                        Nicknames = c.String(),
                        GroupNames = c.String(),
                        BirthDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.UserId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId);
            
            AddColumn("dbo.Users", "PrefferedName", c => c.String());
            AddColumn("dbo.UserAuthDetails", "RegisterDate", c => c.DateTime(nullable: false, defaultValue: DateTime.UtcNow));
            DropColumn("dbo.Users", "PhoneNumber");
            DropColumn("dbo.UserAuthDetails", "LegacyAllRpgInp");
        }
        
        public override void Down()
        {
            AddColumn("dbo.UserAuthDetails", "LegacyAllRpgInp", c => c.Int());
            AddColumn("dbo.Users", "PhoneNumber", c => c.String());
            DropForeignKey("dbo.UserExtras", "UserId", "dbo.Users");
            DropForeignKey("dbo.AllrpgUserDetails", "UserId", "dbo.Users");
            DropIndex("dbo.UserExtras", new[] { "UserId" });
            DropIndex("dbo.AllrpgUserDetails", new[] { "UserId" });
            DropColumn("dbo.UserAuthDetails", "RegisterDate");
            DropColumn("dbo.Users", "PrefferedName");
            DropTable("dbo.UserExtras");
            DropTable("dbo.AllrpgUserDetails");
        }
    }
}
