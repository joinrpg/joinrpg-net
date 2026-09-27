namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Удаляет осиротевшую колонку AccommodationInvites.IsGroupInvite: свойство из сущности
/// давно убрали, а колонку в БД не дропнули (ADR016, задача P2).
/// </summary>
/// <remarks>
/// Колонку добавила миграция 201801280756568_InviteUpdate. В модели соответствующего
/// свойства нет, поэтому снапшот считает, что колонки не существует, — скаффолдер
/// эту миграцию не сгенерирует, она написана руками. Модель не меняется.
///
/// Данные не теряются: на проде 4077 строк, во всех IsGroupInvite = 0, значение 1
/// не встречается ни разу; в коде колонка не читается.
/// </remarks>
public partial class DropOrphanIsGroupInvite : DbMigration
{
    public override void Up()
        // DropColumn сам снимает DEFAULT-констрейнт, имя которого автогенерируемое
        // и на разных окружениях разное (на проде DF__Accommoda__IsGro__6E01572D,
        // на свежей базе — другое), поэтому по имени его дропать нельзя.
        => DropColumn("dbo.AccommodationInvites", "IsGroupInvite");

    public override void Down()
        => AddColumn(
            "dbo.AccommodationInvites",
            "IsGroupInvite",
            c => c.Boolean(nullable: false, defaultValue: false));
}
