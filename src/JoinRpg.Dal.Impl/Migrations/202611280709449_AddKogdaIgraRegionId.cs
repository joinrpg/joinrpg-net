namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

public partial class AddKogdaIgraRegionId : DbMigration
{
    public override void Up()
    {
        AddColumn("dbo.KogdaIgraGames", "RegionId", c => c.Int());
        // Сырой ответ КогдаИгры уже хранится в JsonGameData — заполняем регион из него.
        // NULL остаётся у записей без sub_region_id в JSON (удалённые игры с JsonGameData="{}", заглушки).
        // id вне справочника Region хранится как есть — синхронизация приведёт его к NULL при ближайшем обновлении игры.
        Sql("UPDATE dbo.KogdaIgraGames SET RegionId = TRY_CAST(JSON_VALUE(JsonGameData, '$.sub_region_id') AS int) WHERE ISJSON(JsonGameData) = 1 AND JSON_VALUE(JsonGameData, '$.sub_region_id') IS NOT NULL");
    }

    public override void Down()
    {
        DropColumn("dbo.KogdaIgraGames", "RegionId");
    }
}
