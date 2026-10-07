namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Колонку «Игрок» в сетке ролей теперь можно скрыть (#5290). Значение 0 у ContactsColumn раньше
/// значило «только имя игрока», теперь — «колонки нет»; «только имя» переехало в новое значение 3
/// (<see cref="DomainTypes.ProjectMetadata.PlayerColumnMode"/>). Схема не меняется, только данные:
/// без переноса у всех сохранённых сеток молча пропала бы колонка «Игрок».
/// </summary>
public partial class PlayerColumnMode : DbMigration
{
    public override void Up()
        => Sql("UPDATE dbo.ProjectRolesLists SET ContactsColumn = 3 WHERE ContactsColumn = 0");

    // «Скрытой колонки» до этой миграции не было — ближе всего к ней «только имя», то есть тот же 0.
    public override void Down()
        => Sql("UPDATE dbo.ProjectRolesLists SET ContactsColumn = 0 WHERE ContactsColumn = 3");
}
