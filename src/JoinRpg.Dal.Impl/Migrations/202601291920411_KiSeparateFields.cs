namespace JoinRpg.Dal.Impl.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class KiSeparateFields : DbMigration
    {
        public override void Up()
        {
            Sql("UPDATE dbo.KogdaIgraGames SET LastUpdatedAt = NULL");
            AddColumn("dbo.KogdaIgraGames", "RegionName", c => c.String());
            AddColumn("dbo.KogdaIgraGames", "Begin", c => c.DateTime(nullable: false));
            AddColumn("dbo.KogdaIgraGames", "End", c => c.DateTime(nullable: false));
            AddColumn("dbo.KogdaIgraGames", "MasterGroupName", c => c.String());
            AddColumn("dbo.KogdaIgraGames", "SiteUri", c => c.String());
        }

        public override void Down()
        {
            DropColumn("dbo.KogdaIgraGames", "SiteUri");
            DropColumn("dbo.KogdaIgraGames", "MasterGroupName");
            DropColumn("dbo.KogdaIgraGames", "End");
            DropColumn("dbo.KogdaIgraGames", "Begin");
            DropColumn("dbo.KogdaIgraGames", "RegionName");
        }
    }
}
