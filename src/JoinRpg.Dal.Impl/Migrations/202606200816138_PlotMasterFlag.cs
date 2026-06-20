namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class PlotMasterFlag : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.PlotElements", "IsMasterOnly", c => c.Boolean(nullable: false));
        }

        public override void Down()
        {
            DropColumn("dbo.PlotElements", "IsMasterOnly");
        }
    }
}
