namespace JoinRpg.Dal.Impl.Migrations
{
  using System.Data.Entity.Migrations;
    
    public partial class IntroduceHandout : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PlotElements", "ElementType", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.PlotElements", "ElementType");
        }
    }
}
