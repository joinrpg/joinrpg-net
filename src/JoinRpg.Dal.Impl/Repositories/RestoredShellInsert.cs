namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Вставка «оболочки» потерянной сущности проекта (группы, персонажа) с прежним идентификатором —
/// DRP, #5529. EF6 значение IDENTITY-колонки не вставляет, поэтому это один SQL-батч со
/// <c>SET IDENTITY_INSERT</c>, по образцу оболочки заявки (<c>InsertRestoredClaimShell</c>).
/// </summary>
/// <remarks>
/// <para>
/// Одним батчем и своей транзакцией. Проверка занятости — внутри транзакции с блокировкой диапазона,
/// чтобы два одновременных восстановления одной сущности не столкнулись на первичном ключе.
/// IDENTITY_INSERT действует на сессию, поэтому выключается и при ошибке.
/// </para>
/// <para>
/// Вставляется только id строго ниже текущего значения счётчика (<c>IDENT_CURRENT</c>): такую строку
/// база когда-то уже выдавала (потерянные id лежат ниже сдвинутых после инцидента счётчиков), и явная
/// вставка ниже счётчика его не двигает — на живом сайте это безопасно. Id не ниже счётчика — не
/// потерянная строка; это отказ, а не ошибка, чтобы сервис объяснил его по-человечески. Если счётчик
/// не прочитался (<c>IDENT_CURRENT</c> вернул NULL), сравнение с NULL пропустило бы вставку, поэтому NULL
/// считается нулём — и это тоже отказ.
/// </para>
/// <para>
/// Параметры батча по соглашению: <c>@p0</c> — id, <c>@p1</c> — проект, <c>@p2</c> — корневая группа
/// проекта (батч проверяет, что она корневая и из этого проекта: ссылочной целостности у списков
/// родителей нет). Остальные — у самой вставки.
/// </para>
/// </remarks>
internal static class RestoredShellInsert
{
    public static async Task<RestoredShellInsertResult> Execute(
        MyDbContext ctx,
        string table,
        string idColumn,
        string insertSql,
        params object[] parameters)
    {
        var sql = $"""
            SET XACT_ABORT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                DECLARE @existingProjectId int;
                SELECT @existingProjectId = ProjectId FROM dbo.{table} WITH (UPDLOCK, HOLDLOCK) WHERE {idColumn} = @p0;
                IF @existingProjectId IS NOT NULL
                BEGIN
                    COMMIT TRANSACTION;
                    SELECT CASE WHEN @existingProjectId = @p1 THEN {(int)RestoredShellInsertResult.AlreadyExists} ELSE {(int)RestoredShellInsertResult.TakenInOtherProject} END;
                    RETURN;
                END;
                IF @p0 <= 0 OR @p0 >= ISNULL(IDENT_CURRENT('dbo.{table}'), 0)
                BEGIN
                    COMMIT TRANSACTION;
                    SELECT {(int)RestoredShellInsertResult.NotLostId};
                    RETURN;
                END;
                IF NOT EXISTS (SELECT 1 FROM dbo.CharacterGroups WHERE CharacterGroupId = @p2 AND ProjectId = @p1 AND IsRoot = 1)
                    THROW 50000, N'Parent group is not the root group of the project', 1;
                SET IDENTITY_INSERT dbo.{table} ON;
                {insertSql}
                SET IDENTITY_INSERT dbo.{table} OFF;
                COMMIT TRANSACTION;
                SELECT {(int)RestoredShellInsertResult.Inserted};
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                SET IDENTITY_INSERT dbo.{table} OFF;
                THROW;
            END CATCH;
            """;

        var result = await ctx.Database.SqlQuery<int>(sql, parameters).SingleAsync();
        return (RestoredShellInsertResult)result;
    }
}
