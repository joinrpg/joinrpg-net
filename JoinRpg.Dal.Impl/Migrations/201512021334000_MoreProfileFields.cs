namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class MoreProfileFields : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserExtras", "Vk", c => c.String());
            AddColumn("dbo.UserExtras", "Livejournal", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserExtras", "Livejournal");
            DropColumn("dbo.UserExtras", "Vk");
        }
    }
}
