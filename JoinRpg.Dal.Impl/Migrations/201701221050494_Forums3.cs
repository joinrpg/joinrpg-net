namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Forums3 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ForumThreads", "IsVisibleToPlayer", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ForumThreads", "IsVisibleToPlayer");
        }
    }
}
