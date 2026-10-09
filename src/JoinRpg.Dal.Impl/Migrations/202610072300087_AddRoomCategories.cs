namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Разделение типа проживания и категории комнат (ADR020, §1). Каждый существующий тип получает
/// категорию с тем же Id и тем же именем, поэтому на старых данных идентификаторы не меняются:
/// комната переезжает из типа в одноимённую категорию копированием числа.
/// </summary>
public partial class AddRoomCategories : DbMigration
{
    public override void Up()
    {
        CreateTable(
            "dbo.ProjectRoomCategories",
            c => new
            {
                Id = c.Int(nullable: false, identity: true),
                ProjectId = c.Int(nullable: false),
                Name = c.String(nullable: false),
            })
            .PrimaryKey(t => t.Id)
            .ForeignKey("dbo.Projects", t => t.ProjectId)
            .Index(t => t.ProjectId);

        // Явная вставка в identity-колонку сдвигает её счётчик за наибольший вставленный Id сама,
        // так что следующая новая категория получит свободный номер.
        Sql("""
            SET IDENTITY_INSERT dbo.ProjectRoomCategories ON;
            INSERT INTO dbo.ProjectRoomCategories (Id, ProjectId, Name)
                SELECT Id, ProjectId, Name FROM dbo.ProjectAccommodationTypes;
            SET IDENTITY_INSERT dbo.ProjectRoomCategories OFF;
            """);

        AddColumn("dbo.ProjectAccommodationTypes", "RoomCategoryId", c => c.Int());
        AddColumn("dbo.ProjectAccommodations", "RoomCategoryId", c => c.Int());
        Sql("UPDATE dbo.ProjectAccommodationTypes SET RoomCategoryId = Id");
        Sql("UPDATE dbo.ProjectAccommodations SET RoomCategoryId = AccommodationTypeId");
        AlterColumn("dbo.ProjectAccommodationTypes", "RoomCategoryId", c => c.Int(nullable: false));
        AlterColumn("dbo.ProjectAccommodations", "RoomCategoryId", c => c.Int(nullable: false));

        CreateIndex("dbo.ProjectAccommodations", "RoomCategoryId");
        CreateIndex("dbo.ProjectAccommodationTypes", "RoomCategoryId");
        AddForeignKey("dbo.ProjectAccommodationTypes", "RoomCategoryId", "dbo.ProjectRoomCategories", "Id");
        AddForeignKey("dbo.ProjectAccommodations", "RoomCategoryId", "dbo.ProjectRoomCategories", "Id", cascadeDelete: true);

        DropForeignKey("dbo.ProjectAccommodations", "AccommodationTypeId", "dbo.ProjectAccommodationTypes");
        DropIndex("dbo.ProjectAccommodations", new[] { "AccommodationTypeId" });
        DropColumn("dbo.ProjectAccommodations", "AccommodationTypeId");
    }

    public override void Down()
    {
        // Откат теряет данные, если в категории было больше одного типа: комната вернётся к
        // первому из них. Категорий без типов не бывает — последний тип уносит категорию с собой.
        AddColumn("dbo.ProjectAccommodations", "AccommodationTypeId", c => c.Int());
        Sql("""
            UPDATE room SET AccommodationTypeId =
                (SELECT MIN(type.Id) FROM dbo.ProjectAccommodationTypes type WHERE type.RoomCategoryId = room.RoomCategoryId)
            FROM dbo.ProjectAccommodations room;
            """);
        AlterColumn("dbo.ProjectAccommodations", "AccommodationTypeId", c => c.Int(nullable: false));

        DropForeignKey("dbo.ProjectAccommodations", "RoomCategoryId", "dbo.ProjectRoomCategories");
        DropForeignKey("dbo.ProjectAccommodationTypes", "RoomCategoryId", "dbo.ProjectRoomCategories");
        DropForeignKey("dbo.ProjectRoomCategories", "ProjectId", "dbo.Projects");
        DropIndex("dbo.ProjectRoomCategories", new[] { "ProjectId" });
        DropIndex("dbo.ProjectAccommodationTypes", new[] { "RoomCategoryId" });
        DropIndex("dbo.ProjectAccommodations", new[] { "RoomCategoryId" });
        DropColumn("dbo.ProjectAccommodationTypes", "RoomCategoryId");
        DropColumn("dbo.ProjectAccommodations", "RoomCategoryId");
        DropTable("dbo.ProjectRoomCategories");
        CreateIndex("dbo.ProjectAccommodations", "AccommodationTypeId");
        AddForeignKey("dbo.ProjectAccommodations", "AccommodationTypeId", "dbo.ProjectAccommodationTypes", "Id", cascadeDelete: true);
    }
}
