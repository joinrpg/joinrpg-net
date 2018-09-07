namespace JoinRpg.Dal.Impl.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ClaimUpdate : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Claims", "ClaimDenialStatus", c => c.Int());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Claims", "ClaimDenialStatus");
        }
    }
}
