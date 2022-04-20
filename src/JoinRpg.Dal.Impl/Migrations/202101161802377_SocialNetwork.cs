namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;
    using JoinRpg.DataModel;

    public partial class SocialNetwork : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserExtras", "VkVerified", c => c.Boolean(nullable: false, defaultValue: false));
            AddColumn("dbo.UserExtras", "SocialNetworksAccess",
                c => c.Byte(nullable: false, defaultValue: (byte)ContactsAccessType.OnlyForMasters));
        }

        public override void Down()
        {
            DropColumn("dbo.UserExtras", "SocialNetworksAccess");
            DropColumn("dbo.UserExtras", "VkVerified");
        }
    }
}
