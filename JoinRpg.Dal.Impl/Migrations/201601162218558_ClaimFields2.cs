namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ClaimFields2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Claims", "JsonData", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.Claims", "JsonData");
        }
    }
}
