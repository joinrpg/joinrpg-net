namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AddKogdaIgraGameStatus : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.KogdaIgraGames", "Status", c => c.Int());
        // Сырой ответ КогдаИгры уже хранится в JsonGameData — заполняем статус из него.
        // NULL остаётся у записей без статуса в JSON (удалённые игры с JsonGameData="{}", заглушки).
        Sql("UPDATE dbo.KogdaIgraGames SET Status = TRY_CAST(JSON_VALUE(JsonGameData, '$.status') AS int) WHERE ISJSON(JsonGameData) = 1 AND JSON_VALUE(JsonGameData, '$.status') IS NOT NULL");
    }

    public override void Down()
    {
        DropColumn("dbo.KogdaIgraGames", "Status");
    }
}
