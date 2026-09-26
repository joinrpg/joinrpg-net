namespace JoinRpg.Dal.Impl.Migrations;

using System.Data.Entity.Migrations;

/// <summary>
/// Восстанавливает индексы, которые объявлены в модели и есть на свежесобранной базе,
/// но на проде отсутствуют — их когда-то снесли руками, в обход миграций (ADR015, задача P5).
/// </summary>
/// <remarks>
/// Миграция написана руками: скаффолдер её не сгенерирует, потому что модель эти индексы
/// объявляет (конвенция EF6 создаёт индекс под каждый внешний ключ), то есть с точки зрения
/// снапшота всё в порядке. Расходится только прод.
///
/// Каждый CREATE INDEX обёрнут проверкой существования: на свежей базе индексы уже есть,
/// там миграция — полный no-op, работу она делает только на проде.
/// </remarks>
public partial class RestoreIndexesMissingOnProduction : DbMigration
{
    /// <summary>
    /// Семь из восьми — однокоолоночные индексы по колонке, которая является кластерным
    /// первичным ключом той же таблицы (связи «один к одному по общему ключу»), то есть
    /// строго избыточные. Восьмой, Claims.IX_ResponsibleMasterUserId, — нормальный индекс
    /// по внешнему ключу; он пропал при ручном ALTER COLUMN в 2021 году
    /// (см. закомментированную миграцию 202110241232502_RequireClaimResponsible).
    /// </summary>
    private static readonly (string Table, string Column)[] Indexes =
    [
        ("AllrpgUserDetails", "UserId"),
        ("CommentTexts", "CommentId"),
        ("FinanceOperations", "CommentId"),
        ("ProjectDetails", "ProjectId"),
        ("TransferTexts", "MoneyTransferId"),
        ("UserAuthDetails", "UserId"),
        ("UserExtras", "UserId"),
        ("Claims", "ResponsibleMasterUserId"),
    ];

    public override void Up()
    {
        foreach (var (table, column) in Indexes)
        {
            Sql($"""
                IF NOT EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_{column}' AND object_id = OBJECT_ID('dbo.{table}'))
                    CREATE NONCLUSTERED INDEX [IX_{column}] ON [dbo].[{table}] ([{column}]);
                """);
        }
    }

    public override void Down()
    {
        // Намеренно пусто. Миграция не добавляет новое состояние, а приводит прод к тому,
        // которое модель и остальные окружения считают текущим, — откатывать нечего.
    }
}
