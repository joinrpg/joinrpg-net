/*
  Сдвиг IDENTITY-счётчиков после восстановления основной БД (MSSQL) из старого бэкапа.

  Зачем. После восстановления счётчики продолжат счёт с максимума бэкапа, и новые
  сущности получат id потерянных. Ссылки из выгрузок, писем, Telegram, логов и базы
  уведомлений (Postgres) тогда поведут на чужие персонажи и заявки. Сдвигаем счётчики
  так, чтобы весь диапазон id, выданных между бэкапом и аварией, остался свободным.

  Порядок запуска (сайт, IdPortal и джобы в это время НЕ запущены):
    1. Восстановить бэкап.
    2. Прогнать этот скрипт (@Apply = 1) — ДО миграций: миграции с данными иначе
       вставят строки с id из потерянного диапазона.
    3. Прогнать Joinrpg.Dal.Migrate.
    4. Прогнать скрипт ещё раз: таблицы, созданные миграциями после бэкапа, пусты,
       их счётчики начинаются с 1.
    5. Только после этого запускать приложения.

  Журнал. Каждая сдвинутая таблица записывается в drp.ReseedLog (таблица и новый счётчик).
  Повторный прогон:
    - таблицу из журнала не сдвигает заново, даже если миграции с данными успели вставить
      в неё строки;
    - если счётчик таблицы из журнала оказался НИЖЕ записанного (например, его временно
      опустили, чтобы создать проект с прежним id), возвращает его к записанному значению.

  Как считается новый счётчик для таблицы, которой ещё нет в журнале:
    - есть известный максимум из логов / базы уведомлений (@Known) —
      max(текущий, известный * (1 + @KnownMarginPercent / 100) + @KnownMarginAbs);
    - нет — max(текущий * @Multiplier, текущий + @MinMargin).
    Результат округляется вверх до @RoundTo. Счётчик никогда не уменьшается.
    TargetSeed в @Known задаёт круглое значение вручную вместо расчёта, но не ниже его.
    @Known только поднимает счётчик над известным максимумом; без него работает
    грубая оценка с большим запасом (дыры в id безвредны, коллизии — нет).

  По умолчанию скрипт ничего не меняет и только показывает план (@Apply = 0).
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Apply bit = 0;

DECLARE @Multiplier decimal(9, 2) = 3;
DECLARE @MinMargin bigint = 1000000;
DECLARE @KnownMarginPercent decimal(9, 2) = 20;
DECLARE @KnownMarginAbs bigint = 10000;
DECLARE @RoundTo bigint = 1000;

-- Максимальные id, замеченные после бэкапа (логи k8s, база уведомлений, выгрузки).
-- Имя таблицы — schema.table без скобок, как в колонке TableName плана.
-- TargetSeed — круглое значение счётчика, выбранное вручную; если задано, заменяет расчёт,
-- но не может быть ниже расчётного (иначе скрипт остановится с ошибкой).
DECLARE @Known TABLE (TableName sysname PRIMARY KEY, MaxSeenId bigint NOT NULL, TargetSeed bigint NULL);
-- Значения — максимум id в путях успешных POST-запросов прода (логи k8s 2026-09-10 — 2026-10-07,
-- последняя запись 2026-10-07T22:30Z). Новые id в пути create-запросов не видны, отсюда запас.
INSERT INTO @Known (TableName, MaxSeenId, TargetSeed) VALUES
    (N'dbo.Projects', 1714, 20000),
    (N'dbo.Characters', 136262, 200000),
    (N'dbo.Claims', 119619, 200000),
    (N'dbo.CharacterGroups', 44388, 100000);

-- Пользователи и комментарии — из базы уведомлений (Postgres, joinrpg-prod-notifications).
-- Пока не вписаны, для них работает грубая оценка (текущий * 3, не меньше текущий + 1 млн).
-- Пользователи:
--   SELECT max(greatest("RecipientUserId", "InitiatorUserId")) FROM "Notifications"
--   WHERE "CreatedAt" < '2026-10-07T22:30:49Z';
-- Комментарии (id финансовой операции = id комментария; последний сегмент EntityReference):
--   SELECT max(split_part(rtrim(split_part("EntityReference", '(', 2), ')'), '-', 3)::int)
--   FROM "Notifications"
--   WHERE "EntityReference" LIKE 'ClaimCommentId(%' OR "EntityReference" LIKE 'FinanceOperationId(%'
--      OR "EntityReference" LIKE 'ForumCommentId(%';
-- INSERT INTO @Known (TableName, MaxSeenId, TargetSeed) VALUES
--     (N'dbo.Users', <max>, NULL),
--     (N'dbo.Comments', <max>, NULL);

-- Журнал уже сделанных сдвигов (если скрипт уже применялся к этой базе).
DECLARE @Logged TABLE (TableName sysname PRIMARY KEY, LoggedSeed bigint NOT NULL);
IF OBJECT_ID(N'drp.ReseedLog', N'U') IS NOT NULL
    INSERT INTO @Logged (TableName, LoggedSeed)
    EXEC sp_executesql N'SELECT TableName, NewSeed FROM drp.ReseedLog;';

DECLARE @Plan TABLE (
    TableName sysname PRIMARY KEY,
    QuotedName nvarchar(300) NOT NULL,
    TypeName sysname NOT NULL,
    CurrentIdent bigint NOT NULL,
    MaxId bigint NULL,
    KnownMax bigint NULL,
    TargetSeed bigint NULL,
    LoggedSeed bigint NULL,
    CalculatedSeed bigint NULL,
    NewSeed bigint NULL,
    Action nvarchar(20) NULL,
    TypeLimit bigint NOT NULL);

INSERT INTO @Plan (TableName, QuotedName, TypeName, CurrentIdent, KnownMax, TargetSeed, LoggedSeed, TypeLimit)
SELECT
    s.name + N'.' + t.name,
    QUOTENAME(s.name) + N'.' + QUOTENAME(t.name),
    ty.name,
    CAST(IDENT_CURRENT(QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)) AS bigint),
    k.MaxSeenId,
    k.TargetSeed,
    l.LoggedSeed,
    CASE ty.name
        WHEN N'tinyint' THEN 255
        WHEN N'smallint' THEN 32767
        WHEN N'int' THEN 2147483647
        ELSE 9223372036854775807
    END
FROM sys.identity_columns ic
JOIN sys.tables t ON t.object_id = ic.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.types ty ON ty.user_type_id = ic.user_type_id
LEFT JOIN @Known k ON k.TableName = s.name + N'.' + t.name
LEFT JOIN @Logged l ON l.TableName = s.name + N'.' + t.name
WHERE t.is_ms_shipped = 0 AND s.name <> N'drp';

-- Реальный максимум id: IDENT_CURRENT может отставать, если кто-то вставлял с IDENTITY_INSERT.
DECLARE @TableName sysname, @QuotedName nvarchar(300), @Column sysname, @Sql nvarchar(max), @MaxId bigint;
DECLARE max_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT p.TableName, p.QuotedName, ic.name
    FROM @Plan p
    JOIN sys.identity_columns ic ON ic.object_id = OBJECT_ID(p.QuotedName);
OPEN max_cursor;
FETCH NEXT FROM max_cursor INTO @TableName, @QuotedName, @Column;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @Sql = N'SELECT @MaxId = CAST(MAX(' + QUOTENAME(@Column) + N') AS bigint) FROM ' + @QuotedName + N';';
    EXEC sp_executesql @Sql, N'@MaxId bigint OUTPUT', @MaxId = @MaxId OUTPUT;
    UPDATE @Plan SET MaxId = @MaxId WHERE TableName = @TableName;
    FETCH NEXT FROM max_cursor INTO @TableName, @QuotedName, @Column;
END;
CLOSE max_cursor;
DEALLOCATE max_cursor;

UPDATE p
SET CalculatedSeed = CEILING(CAST(x.Raw AS decimal(38, 2)) / @RoundTo) * @RoundTo
FROM @Plan p
CROSS APPLY (SELECT CASE WHEN p.MaxId > p.CurrentIdent THEN p.MaxId ELSE p.CurrentIdent END AS Base) b
CROSS APPLY (
    SELECT CASE
        WHEN p.KnownMax IS NOT NULL THEN
            (SELECT MAX(v) FROM (VALUES
                (b.Base),
                (CAST(CEILING(p.KnownMax * (1 + @KnownMarginPercent / 100)) AS bigint) + @KnownMarginAbs)) AS vals(v))
        ELSE
            (SELECT MAX(v) FROM (VALUES
                (CAST(CEILING(b.Base * @Multiplier) AS bigint)),
                (b.Base + @MinMargin)) AS vals(v))
    END AS Raw) x
WHERE p.LoggedSeed IS NULL;

-- Что делать с каждой таблицей:
--   Shift   — ещё не сдвигалась: новый счётчик по расчёту или TargetSeed;
--   Restore — уже сдвигалась, но счётчик ниже записанного: вернуть записанное значение;
--   Skip    — уже сдвинута, счётчик не ниже записанного.
UPDATE @Plan SET
    Action = CASE
        WHEN LoggedSeed IS NULL THEN N'Shift'
        WHEN CurrentIdent < LoggedSeed THEN N'Restore'
        ELSE N'Skip'
    END,
    NewSeed = CASE
        WHEN LoggedSeed IS NULL THEN ISNULL(TargetSeed, CalculatedSeed)
        WHEN CurrentIdent < LoggedSeed THEN LoggedSeed
    END;

SELECT
    p.TableName,
    p.TypeName,
    p.MaxId,
    p.CurrentIdent,
    p.KnownMax,
    p.LoggedSeed,
    p.CalculatedSeed,
    p.NewSeed,
    p.Action,
    CASE
        WHEN p.Action = N'Shift' AND p.NewSeed < p.CalculatedSeed THEN N'ОШИБКА: TargetSeed ниже расчётного'
        WHEN p.Action = N'Restore' AND p.MaxId >= p.LoggedSeed THEN N'ОШИБКА: в таблице есть id не ниже записанного счётчика'
        WHEN p.Action <> N'Skip' AND p.NewSeed > p.TypeLimit / 2 THEN N'ОШИБКА: больше половины диапазона типа'
        WHEN p.Action = N'Skip' THEN N'Уже сдвинуто'
        WHEN p.Action = N'Restore' THEN N'Счётчик ниже записанного — вернуть'
        ELSE N''
    END AS Note
FROM @Plan p
ORDER BY p.TableName;

IF EXISTS (SELECT 1 FROM @Plan WHERE Action <> N'Skip' AND NewSeed > TypeLimit / 2)
BEGIN
    RAISERROR(N'Новый счётчик некоторых таблиц больше половины диапазона их типа. Уменьшите @Multiplier/@MinMargin или задайте @Known.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM @Plan WHERE Action = N'Shift' AND NewSeed < CalculatedSeed)
BEGIN
    RAISERROR(N'TargetSeed некоторых таблиц ниже расчётного минимума. Поднимите TargetSeed.', 16, 1);
    RETURN;
END;

IF EXISTS (SELECT 1 FROM @Plan WHERE Action = N'Restore' AND MaxId >= LoggedSeed)
BEGIN
    RAISERROR(N'В таблице с опущенным счётчиком есть id не ниже записанного: возврат счётчика дал бы коллизию. Разобраться вручную.', 16, 1);
    RETURN;
END;

IF @Apply = 0
BEGIN
    PRINT N'@Apply = 0: только план, ничего не изменено.';
    RETURN;
END;

IF SCHEMA_ID(N'drp') IS NULL
    EXEC (N'CREATE SCHEMA drp;');
IF OBJECT_ID(N'drp.ReseedLog', N'U') IS NULL
    EXEC (N'CREATE TABLE drp.ReseedLog (
        TableName sysname NOT NULL PRIMARY KEY,
        OldIdent bigint NOT NULL,
        NewSeed bigint NOT NULL,
        ShiftedAt datetime2 NOT NULL DEFAULT SYSUTCDATETIME());');

DECLARE @NewSeed bigint, @OldIdent bigint, @Action nvarchar(20);
DECLARE apply_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT p.TableName, p.QuotedName, p.NewSeed, p.CurrentIdent, p.Action
    FROM @Plan p
    WHERE p.Action = N'Restore'
       OR (p.Action = N'Shift' AND p.NewSeed > p.CurrentIdent);

BEGIN TRANSACTION;
OPEN apply_cursor;
FETCH NEXT FROM apply_cursor INTO @TableName, @QuotedName, @NewSeed, @OldIdent, @Action;
WHILE @@FETCH_STATUS = 0
BEGIN
    DBCC CHECKIDENT (@QuotedName, RESEED, @NewSeed) WITH NO_INFOMSGS;
    IF @Action = N'Shift'
        EXEC sp_executesql
            N'INSERT INTO drp.ReseedLog (TableName, OldIdent, NewSeed) VALUES (@t, @o, @n);',
            N'@t sysname, @o bigint, @n bigint', @t = @TableName, @o = @OldIdent, @n = @NewSeed;
    PRINT CONCAT(@Action, N' ', @QuotedName, N': ', @OldIdent, N' -> ', @NewSeed);
    FETCH NEXT FROM apply_cursor INTO @TableName, @QuotedName, @NewSeed, @OldIdent, @Action;
END;
CLOSE apply_cursor;
DEALLOCATE apply_cursor;
COMMIT TRANSACTION;

PRINT N'Готово.';
