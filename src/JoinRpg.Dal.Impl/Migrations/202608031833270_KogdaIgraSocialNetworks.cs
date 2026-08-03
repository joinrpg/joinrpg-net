namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class KogdaIgraSocialNetworks : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.KogdaIgraGames", "VkClub", c => c.String());
        AddColumn("dbo.KogdaIgraGames", "LjComm", c => c.String());
        AddColumn("dbo.KogdaIgraGames", "TelegramChannel", c => c.String());
    }

    public override void Down()
    {
        DropColumn("dbo.KogdaIgraGames", "TelegramChannel");
        DropColumn("dbo.KogdaIgraGames", "LjComm");
        DropColumn("dbo.KogdaIgraGames", "VkClub");
    }
}
