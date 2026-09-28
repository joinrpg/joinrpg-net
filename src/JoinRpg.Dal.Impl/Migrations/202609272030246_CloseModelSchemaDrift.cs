namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Закрывает два последних расхождения модели со схемой из сверки ADR016 (задача P2).
/// </summary>
/// <remarks>
/// Обе колонки — не дрейф прода: прод и свежесобранная база согласны между собой,
/// расходилась с ними модель.
///
/// <para>
/// <c>UserAuthDetails.AspNetSecurityStamp</c>: в БД NOT NULL с миграции 201904181355184,
/// а на свойстве не было <c>[Required]</c>. Атрибут добавлен, эту часть миграции
/// сгенерировал скаффолдер. На проде колонка уже NOT NULL, так что DDL там ничего
/// не меняет.
/// </para>
/// <para>
/// <c>ProjectFieldDropdownValues.PlayerSelectable</c>: миграция 201801111633386 добавила
/// колонку как <c>nullable: true</c> (её отредактировали руками, чтобы прошёл бэкфилл),
/// а завершающий AlterColumn забыли — при том что свойство в модели всегда было
/// non-nullable <c>bool</c>. Снапшот считает колонку NOT NULL, поэтому скаффолдер эту
/// часть не сгенерирует, она дописана руками.
/// </para>
/// <para>
/// Бэкфилла нет: на проде 0 NULL-ов из 15195 строк (проверено), на свежей базе строк
/// нет вообще. Если NULL-ы найдутся на чьей-то девелоперской базе, миграция упадёт
/// громко — это лучше, чем молча проставить произвольное значение видимости варианта.
/// Индексов ни на одной из колонок нет, так что ALTER COLUMN не встретит ошибку 4922.
/// </para>
/// </remarks>
public partial class CloseModelSchemaDrift : DbMigration
{
    public override void Up()
    {
        AlterColumn("dbo.UserAuthDetails", "AspNetSecurityStamp", c => c.String(nullable: false));
        AlterColumn("dbo.ProjectFieldDropdownValues", "PlayerSelectable", c => c.Boolean(nullable: false));
    }

    public override void Down()
    {
        AlterColumn("dbo.ProjectFieldDropdownValues", "PlayerSelectable", c => c.Boolean());
        AlterColumn("dbo.UserAuthDetails", "AspNetSecurityStamp", c => c.String());
    }
}
