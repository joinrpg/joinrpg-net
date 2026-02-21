namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class KiNullableDates : DbMigration
{
    public override void Up()
    {
        AlterColumn("dbo.KogdaIgraGames", "Begin", c => c.DateTime());
        AlterColumn("dbo.KogdaIgraGames", "End", c => c.DateTime());
        Sql("UPDATE dbo.KogdaIgraGames SET LastUpdatedAt = NULL, [Begin] = NULL, [End] = NULL");
    }

    public override void Down()
    {
        AlterColumn("dbo.KogdaIgraGames", "End", c => c.DateTime(nullable: false));
        AlterColumn("dbo.KogdaIgraGames", "Begin", c => c.DateTime(nullable: false));
    }
}
