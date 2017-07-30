namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class ReplaceFeeStartWithFeeEnd : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ProjectFeeSettings", "StartDate", c => c.DateTime(nullable: false));
            DropColumn("dbo.ProjectFeeSettings", "EndDate");
        }
        
        public override void Down()
        {
            AddColumn("dbo.ProjectFeeSettings", "EndDate", c => c.DateTime(nullable: false));
            DropColumn("dbo.ProjectFeeSettings", "StartDate");
        }
    }
}
