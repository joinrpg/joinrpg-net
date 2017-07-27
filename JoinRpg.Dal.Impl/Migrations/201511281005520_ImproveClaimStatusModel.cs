namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ImproveClaimStatusModel : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Claims", "LastUpdateDateTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.Claims", "ClaimStatus", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Claims", "ClaimStatus");
            DropColumn("dbo.Claims", "LastUpdateDateTime");
        }
    }
}
