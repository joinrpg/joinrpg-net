namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class Telegram : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.UserExtras", "Telegram", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.UserExtras", "Telegram");
        }
    }
}
