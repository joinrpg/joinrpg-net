namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Доводит до конца то, что должна была сделать миграция 202110241232502_RequireClaimResponsible:
/// переводит Claims.ResponsibleMasterUserId в NOT NULL (ADR015, задача P5).
/// </summary>
/// <remarks>
/// История расхождения: в 2021 году свойство в модели сделали non-nullable, но у миграции,
/// которая должна была выровнять схему, <c>Up()</c> целиком закомментирован — автор упёрся
/// в покрывающие индексы, мешавшие ALTER COLUMN. При этом .resx отскаффолдился от новой
/// модели, поэтому снапшот EF6 считает колонку NOT NULL и исправляющую миграцию
/// скаффолдер никогда не сгенерирует. На проде колонку впоследствии выровняли руками,
/// в обход миграций, — и получилось, что прод правильный, а любая свежесобранная база нет.
///
/// Блокера 2021 года больше нет: nci_wi_Claims_DD27A24D… дропнут миграцией
/// 202411191331034_RemoveGroupClaims, а Claim_Cover_index в миграциях никогда не создавался.
/// Сейчас колонку держит только узкий IX_ResponsibleMasterUserId — его и достаточно снять.
/// Внешний ключ снимать не требуется: проверено, ALTER COLUMN с ним проходит
/// (без снятия индекса — падает с ошибкой 4922).
///
/// Бэкфилла сознательно нет. На проде NULL-ов ноль (колонка там уже NOT NULL), на свежей
/// базе строк нет вообще, а придумывать ответственного мастера за пользователя миграция
/// не должна. Если NULL-ы найдутся на чьей-то девелоперской базе, ALTER упадёт громко
/// с ошибкой 515 — это лучше, чем молча назначить произвольного мастера, тем более что
/// ClaimListBuilder обращается к найденному мастеру без проверки на null.
///
/// Модель и снапшот не меняются, миграция рукописная.
/// </remarks>
public partial class ClaimsResponsibleMasterNotNull : DbMigration
{
    public override void Up()
        // Всё под проверкой nullability: на проде колонка уже NOT NULL, там миграция — no-op.
        => Sql("""
            IF EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.Claims')
                  AND name = 'ResponsibleMasterUserId' AND is_nullable = 1)
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_ResponsibleMasterUserId' AND object_id = OBJECT_ID('dbo.Claims'))
                    DROP INDEX [IX_ResponsibleMasterUserId] ON [dbo].[Claims];

                ALTER TABLE [dbo].[Claims] ALTER COLUMN [ResponsibleMasterUserId] int NOT NULL;

                CREATE NONCLUSTERED INDEX [IX_ResponsibleMasterUserId]
                    ON [dbo].[Claims] ([ResponsibleMasterUserId]);
            END
            """);

    public override void Down()
        => Sql("""
            IF EXISTS (
                SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.Claims')
                  AND name = 'ResponsibleMasterUserId' AND is_nullable = 0)
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_ResponsibleMasterUserId' AND object_id = OBJECT_ID('dbo.Claims'))
                    DROP INDEX [IX_ResponsibleMasterUserId] ON [dbo].[Claims];

                ALTER TABLE [dbo].[Claims] ALTER COLUMN [ResponsibleMasterUserId] int NULL;

                CREATE NONCLUSTERED INDEX [IX_ResponsibleMasterUserId]
                    ON [dbo].[Claims] ([ResponsibleMasterUserId]);
            END
            """);
}
