using JoinRpg.Dal.Impl.Repositories.Accommodation;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;

namespace JoinRpg.Dal.Impl.Repositories.Characters;

/// <summary>
/// Реализация <see cref="ICharacterAggregateWriteRepository"/> (ADR014).
/// </summary>
/// <remarks>
/// Не регистрируется в DI (см. <c>Registraton</c>): создаётся только из <c>MyDbContext</c>,
/// чтобы трекинг и <c>SaveChanges</c> шли через один и тот же контекст.
/// </remarks>
internal class CharacterAggregateWriteRepository(MyDbContext ctx) : ICharacterAggregateWriteRepository
{
    private readonly CharacterInfoLoader loader = new(ctx);

    public async Task<ICharacterAggregateUpdateHandle> LoadCharacterForUpdate(
        CharacterIdentification characterId)
    {
        var (project, projectInfo) = await LoadProject(characterId.ProjectId);

        var character = await LoadCharacterEntity(characterId);
        var characterInfo = await LoadCharacterInfo(characterId, projectInfo);

        return new CharacterAggregateUpdateHandle(
            ctx, loader, project, projectInfo, character, characterInfo);
    }

    public async Task<IClaimUpdateHandle> LoadClaimForUpdate(ClaimIdentification claimId)
    {
        var (project, projectInfo) = await LoadProject(claimId.ProjectId);

        var claim = await LoadClaimEntity(claimId);

        var characterId = claim.GetCharacterId();
        var character = await LoadCharacterEntity(characterId);
        var characterInfo = await LoadCharacterInfo(characterId, projectInfo);

        var claimInfo = characterInfo.Claims.SingleOrDefault(c => c.ClaimId == claimId)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

        return new ClaimUpdateHandle(
            ctx, loader, project, projectInfo, character, characterInfo, claim, claimInfo);
    }

    public async Task<bool> InsertRestoredClaimShell(RestoredClaimShell shell)
    {
        // Одним батчем и своей транзакцией: обсуждение получает обычный id, заявка — заданный.
        // Проверка занятости — внутри транзакции с блокировкой диапазона, чтобы два одновременных
        // восстановления одной заявки не столкнулись на первичном ключе. Персонаж обязан быть из того
        // же проекта: внешний ключ этого не проверяет, а заявку на чужом персонаже потом не загрузить.
        // IDENTITY_INSERT действует на сессию, поэтому выключается и при ошибке.
        const string sql = """
            SET XACT_ABORT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                IF EXISTS (SELECT 1 FROM dbo.Claims WITH (UPDLOCK, HOLDLOCK) WHERE ClaimId = @p0)
                BEGIN
                    COMMIT TRANSACTION;
                    SELECT 0;
                    RETURN;
                END;
                IF NOT EXISTS (SELECT 1 FROM dbo.Characters WHERE CharacterId = @p2 AND ProjectId = @p1)
                    THROW 50000, N'Character is not in the claim project', 1;
                INSERT INTO dbo.CommentDiscussions (ProjectId) VALUES (@p1);
                DECLARE @discussionId int = CAST(SCOPE_IDENTITY() AS int);
                SET IDENTITY_INSERT dbo.Claims ON;
                INSERT INTO dbo.Claims (ClaimId, ProjectId, CharacterId, PlayerUserId, ResponsibleMasterUserId,
                    ClaimStatus, CommentDiscussionId, CreateDate, PlayerAcceptedDate, LastUpdateDateTime,
                    PreferentialFeeUser, PlayerAllowedSenstiveData)
                VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @discussionId, @p6, @p6, @p6, 0, 0);
                SET IDENTITY_INSERT dbo.Claims OFF;
                COMMIT TRANSACTION;
                SELECT 1;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                SET IDENTITY_INSERT dbo.Claims OFF;
                THROW;
            END CATCH;
            """;

        var inserted = await ctx.Database.SqlQuery<int>(
                sql,
                shell.ClaimId.ClaimId,
                shell.ClaimId.ProjectId.Value,
                shell.CharacterId.CharacterId,
                shell.PlayerId.Value,
                shell.ResponsibleMasterId.Value,
                (int)ClaimStatus.AddedByMaster,
                shell.CreatedAt)
            .SingleAsync();
        return inserted == 1;
    }

    public Task<bool> IsClaimIdTaken(int claimId) => ctx.Set<Claim>().AnyAsync(c => c.ClaimId == claimId);

    public async Task<int> RevokeSensitiveDataConsents(ProjectIdentification projectId)
    {
        var projectIntId = projectId.Value;
        var consented = await ctx.Set<Claim>()
            .Where(c => c.ProjectId == projectIntId && c.PlayerAllowedSenstiveData)
            .ToListAsync();
        foreach (var claim in consented)
        {
            claim.PlayerAllowedSenstiveData = false;
        }
        return consented.Count;
    }

    public Task<bool> ClaimExists(ClaimIdentification claimId)
    {
        var claimIntId = claimId.ClaimId;
        var projectIntId = claimId.ProjectId.Value;
        return ctx.Set<Claim>().AnyAsync(c => c.ClaimId == claimIntId && c.ProjectId == projectIntId);
    }

    public Task<RestoredShellInsertResult> InsertRestoredCharacterShell(RestoredCharacterShell shell)
    {
        if (shell.CharacterId.ProjectId != shell.RootGroupId.ProjectId)
        {
            throw new ArgumentException($"Корневая группа {shell.RootGroupId} не из проекта персонажа {shell.CharacterId}", nameof(shell));
        }
        if (shell.IsHot && shell.CharacterType == CharacterType.NonPlayer)
        {
            throw new ArgumentException($"NPC {shell.CharacterId} не бывает горячим", nameof(shell));
        }
        var (isPublic, hidePlayerForCharacter) = CharacterTypeInfo.GetVisibilityFlags(shell.Visibility);

        // Поля — как у доменного создания (CharacterServiceImpl.AddCharacter → ApplySettings и
        // SaveFields с пустым слоем): тип, «горячесть» и видимость из оболочки (флаги видимости — как
        // их пишет ApplySettings), заявки принимает (IsAcceptingClaims = 1 у всех, кроме NPC: выгрузка
        // мастера флаг «игрок может подать заявку» не несёт, восстанавливаем значение по умолчанию),
        // без лимита слота, активный; поля — пустой JSON-объект, как сериализует пустой слой;
        // группы — только корневая; аудит — сейчас. Порядок персонажей в группе не трогается.
        // JSON полей — параметром: EF6 прогоняет текст команды через string.Format, и фигурные скобки
        // в литерале её ломают.
        const string insert = """
            INSERT INTO dbo.Characters (CharacterId, ProjectId, CharacterName, CharacterType, IsPublic,
                HidePlayerForCharacter, IsAcceptingClaims, IsHot, CharacterSlotLimit, IsActive, InGame, AutoCreated,
                JsonData, Description_Contents, ParentGroupsImpl_ListIds, PlotElementOrderData, ApprovedClaimId,
                OriginalCharacterSlot_CharacterId, CreatedAt, CreatedById, UpdatedAt, UpdatedById)
            VALUES (@p0, @p1, @p3, @p4, @p11,
                @p12, @p9, @p10, NULL, 1, 0, 0,
                @p8, @p5, CAST(@p2 AS nvarchar(20)), NULL, NULL,
                NULL, @p6, @p7, @p6, @p7);
            """;

        return RestoredShellInsert.Execute(
            ctx,
            "Characters",
            "CharacterId",
            insert,
            shell.CharacterId.CharacterId,
            shell.CharacterId.ProjectId.Value,
            shell.RootGroupId.CharacterGroupId,
            shell.CharacterName,
            (int)shell.CharacterType,
            (object?)shell.Description ?? DBNull.Value,
            shell.CreatedAt,
            shell.CreatedBy.Value,
            EmptyFieldsJson,
            shell.CharacterType != CharacterType.NonPlayer,
            shell.IsHot,
            isPublic,
            hidePlayerForCharacter);
    }

    public async Task<RestoredCommentInsertResult> InsertRestoredClaimComment(RestoredClaimCommentRow comment)
    {
        // Отдельный батч, а не RestoredShellInsert: занятость id здесь решает обсуждение, а не проект,
        // проверки корневой группы нет, а без id — обычная вставка с поиском повтора.
        // Одним батчем и своей транзакцией; проверки занятости — с блокировкой диапазона, чтобы два
        // одновременных восстановления одного комментария не вставили его дважды. Колонки — как у
        // доменного создания (CommentHelper.CreateCommentForDiscussion): родитель — только у решения по
        // финансовой операции (сама операция, из этого же обсуждения), время правки равно времени создания. Повтор без id узнаётся по автору, времени и тексту; текст сравнивается
        // побайтно, а не по сортировке базы, нечувствительной к регистру. Текст и прочее — параметрами:
        // EF6 прогоняет текст команды через string.Format, фигурные скобки markdown его сломали бы.
        // IDENTITY_INSERT действует на сессию, поэтому выключается и при ошибке.
        // Время приводится к типу колонки (datetime, шаг ~3 мс) один раз: и пишется, и сравнивается при
        // поиске повтора одно и то же значение, как бы провайдер ни типизировал параметр.
        if (!await ClaimExists(comment.ClaimId))
        {
            // То же исключение, что у загрузки заявки; THROW в батче — только на гонку с удалением.
            throw new JoinRpgEntityNotFoundException(comment.ClaimId.ClaimId, "claim");
        }

        var sql = $"""
            SET XACT_ABORT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                DECLARE @createdAt datetime = @p7;
                DECLARE @discussionId int;
                SELECT @discussionId = CommentDiscussionId FROM dbo.Claims WHERE ClaimId = @p0 AND ProjectId = @p1;
                IF @discussionId IS NULL
                    THROW 50000, N'Claim is not found in the project', 1;
                DECLARE @parentId int = @p9;
                IF @parentId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Comments WHERE CommentId = @parentId AND CommentDiscussionId = @discussionId)
                    THROW 50000, N'Parent comment is not found in the claim discussion', 1;
                DECLARE @commentId int = @p2;
                IF @commentId IS NOT NULL
                BEGIN
                    DECLARE @existingDiscussionId int;
                    SELECT @existingDiscussionId = CommentDiscussionId FROM dbo.Comments WITH (UPDLOCK, HOLDLOCK) WHERE CommentId = @commentId;
                    IF @existingDiscussionId IS NOT NULL
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT CASE WHEN @existingDiscussionId = @discussionId THEN {(int)RestoredCommentInsertResult.AlreadyExists} ELSE {(int)RestoredCommentInsertResult.TakenElsewhere} END;
                        RETURN;
                    END;
                    IF @commentId <= 0 OR @commentId >= ISNULL(IDENT_CURRENT('dbo.Comments'), 0)
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT {(int)RestoredCommentInsertResult.NotLostId};
                        RETURN;
                    END;
                    SET IDENTITY_INSERT dbo.Comments ON;
                    INSERT INTO dbo.Comments (CommentId, ProjectId, CommentDiscussionId, ParentCommentId, CreatedTime,
                        LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer, ExtraAction)
                    VALUES (@commentId, @p1, @discussionId, @parentId, @createdAt, @createdAt, @p3, @p4, @p5, @p6);
                    SET IDENTITY_INSERT dbo.Comments OFF;
                END
                ELSE
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM dbo.Comments c WITH (UPDLOCK, HOLDLOCK)
                        JOIN dbo.CommentTexts t ON t.CommentId = c.CommentId
                        WHERE c.CommentDiscussionId = @discussionId AND c.AuthorUserId = @p3 AND c.CreatedTime = @createdAt
                            AND CAST(t.Text_Contents AS varbinary(max)) = CAST(@p8 AS varbinary(max)))
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT {(int)RestoredCommentInsertResult.AlreadyExists};
                        RETURN;
                    END;
                    INSERT INTO dbo.Comments (ProjectId, CommentDiscussionId, ParentCommentId, CreatedTime,
                        LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer, ExtraAction)
                    VALUES (@p1, @discussionId, @parentId, @createdAt, @createdAt, @p3, @p4, @p5, @p6);
                    SET @commentId = CAST(SCOPE_IDENTITY() AS int);
                END;
                INSERT INTO dbo.CommentTexts (CommentId, Text_Contents) VALUES (@commentId, @p8);
                COMMIT TRANSACTION;
                SELECT {(int)RestoredCommentInsertResult.Inserted};
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                SET IDENTITY_INSERT dbo.Comments OFF;
                THROW;
            END CATCH;
            """;

        var result = await ctx.Database.SqlQuery<int>(
                sql,
                comment.ClaimId.ClaimId,
                comment.ClaimId.ProjectId.Value,
                (object?)comment.CommentId ?? DBNull.Value,
                comment.AuthorId.Value,
                comment.IsCommentByPlayer,
                comment.IsVisibleToPlayer,
                comment.ExtraAction is { } extraAction ? (object)(int)extraAction : DBNull.Value,
                comment.CreatedAt,
                comment.Text,
                (object?)comment.ParentCommentId ?? DBNull.Value)
            .SingleAsync();
        return (RestoredCommentInsertResult)result;
    }

    public async Task<RestoredFinanceOperationInsert> InsertRestoredFinanceOperation(RestoredFinanceOperationRow operation)
    {
        // Тот же каркас, что у InsertRestoredClaimComment: один батч своей транзакцией, проверки занятости
        // с блокировкой диапазона, IDENTITY_INSERT выключается и при ошибке, текст и прочее — параметрами,
        // время приводится к datetime один раз. Строки — как у доменного приёма взноса (AcceptFee):
        // комментарий без родителя, видимый игроку, с действием по виду операции, и операция с тем же
        // идентификатором (FinanceOperations.CommentId — не IDENTITY). Прежний id «уже есть», только
        // если это операция этого обсуждения: комментарий без операции — занятый номер. Без id повтор
        // узнаётся по операции того же автора, вида и суммы в пределах минуты от времени уведомления.
        if (!await ClaimExists(operation.ClaimId))
        {
            throw new JoinRpgEntityNotFoundException(operation.ClaimId.ClaimId, "claim");
        }
        if (operation.PaymentTypeId.ProjectId != operation.ClaimId.ProjectId)
        {
            throw new ArgumentException($"Тип оплаты {operation.PaymentTypeId} не из проекта заявки {operation.ClaimId}", nameof(operation));
        }
        var extraAction = operation.OperationType switch
        {
            FinanceOperationType.Submit => CommentExtraAction.PaidFee,
            FinanceOperationType.Refund => CommentExtraAction.RefundFee,
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation.OperationType, "Восстанавливаются только взносы и возвраты"),
        };

        var sql = $"""
            SET XACT_ABORT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                DECLARE @createdAt datetime = @p6;
                DECLARE @changedAt datetime = @p12;
                DECLARE @discussionId int;
                SELECT @discussionId = CommentDiscussionId FROM dbo.Claims WHERE ClaimId = @p0 AND ProjectId = @p1;
                IF @discussionId IS NULL
                    THROW 50000, N'Claim is not found in the project', 1;
                IF NOT EXISTS (SELECT 1 FROM dbo.PaymentTypes WHERE PaymentTypeId = @p10 AND ProjectId = @p1)
                    THROW 50000, N'Payment type is not found in the project', 1;
                DECLARE @commentId int = @p2;
                IF @commentId IS NOT NULL
                BEGIN
                    DECLARE @existingDiscussionId int;
                    SELECT @existingDiscussionId = CommentDiscussionId FROM dbo.Comments WITH (UPDLOCK, HOLDLOCK) WHERE CommentId = @commentId;
                    IF @existingDiscussionId IS NOT NULL
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT CASE
                                WHEN @existingDiscussionId = @discussionId AND EXISTS (SELECT 1 FROM dbo.FinanceOperations WHERE CommentId = @commentId)
                                THEN {(int)RestoredCommentInsertResult.AlreadyExists}
                                ELSE {(int)RestoredCommentInsertResult.TakenElsewhere}
                            END AS Result,
                            @commentId AS OperationId;
                        RETURN;
                    END;
                    IF @commentId <= 0 OR @commentId >= ISNULL(IDENT_CURRENT('dbo.Comments'), 0)
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT {(int)RestoredCommentInsertResult.NotLostId} AS Result, @commentId AS OperationId;
                        RETURN;
                    END;
                    SET IDENTITY_INSERT dbo.Comments ON;
                    INSERT INTO dbo.Comments (CommentId, ProjectId, CommentDiscussionId, ParentCommentId, CreatedTime,
                        LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer, ExtraAction)
                    VALUES (@commentId, @p1, @discussionId, NULL, @createdAt, @createdAt, @p3, @p4, 1, @p5);
                    SET IDENTITY_INSERT dbo.Comments OFF;
                END
                ELSE
                BEGIN
                    DECLARE @existingId int;
                    SELECT TOP 1 @existingId = c.CommentId
                    FROM dbo.Comments c WITH (UPDLOCK, HOLDLOCK)
                    JOIN dbo.FinanceOperations f ON f.CommentId = c.CommentId
                    WHERE c.CommentDiscussionId = @discussionId AND c.AuthorUserId = @p3
                        AND c.CreatedTime BETWEEN DATEADD(minute, -1, @createdAt) AND DATEADD(minute, 1, @createdAt)
                        AND f.OperationType = @p8 AND f.MoneyAmount = @p9
                    ORDER BY ABS(DATEDIFF(millisecond, c.CreatedTime, @createdAt)), c.CommentId;
                    IF @existingId IS NOT NULL
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT {(int)RestoredCommentInsertResult.AlreadyExists} AS Result, @existingId AS OperationId;
                        RETURN;
                    END;
                    INSERT INTO dbo.Comments (ProjectId, CommentDiscussionId, ParentCommentId, CreatedTime,
                        LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer, ExtraAction)
                    VALUES (@p1, @discussionId, NULL, @createdAt, @createdAt, @p3, @p4, 1, @p5);
                    SET @commentId = CAST(SCOPE_IDENTITY() AS int);
                END;
                INSERT INTO dbo.CommentTexts (CommentId, Text_Contents) VALUES (@commentId, @p7);
                INSERT INTO dbo.FinanceOperations (CommentId, ProjectId, ClaimId, MoneyAmount, PaymentTypeId, Created,
                    Changed, OperationDate, State, OperationType)
                VALUES (@commentId, @p1, @p0, @p9, @p10, @createdAt, @changedAt, @p13, @p11, @p8);
                COMMIT TRANSACTION;
                SELECT {(int)RestoredCommentInsertResult.Inserted} AS Result, @commentId AS OperationId;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                SET IDENTITY_INSERT dbo.Comments OFF;
                THROW;
            END CATCH;
            """;

        var row = await ctx.Database.SqlQuery<RestoredFinanceOperationInsertRow>(
                sql,
                operation.ClaimId.ClaimId,
                operation.ClaimId.ProjectId.Value,
                (object?)operation.OperationId ?? DBNull.Value,
                operation.AuthorId.Value,
                operation.IsCommentByPlayer,
                (int)extraAction,
                operation.CreatedAt,
                operation.Text,
                (int)operation.OperationType,
                operation.MoneyAmount,
                operation.PaymentTypeId.PaymentTypeId,
                (int)operation.State,
                operation.ChangedAt,
                operation.OperationDate)
            .SingleAsync();
        return new RestoredFinanceOperationInsert((RestoredCommentInsertResult)row.Result, row.OperationId);
    }

    /// <summary>Строка ответа батча <see cref="InsertRestoredFinanceOperation"/>.</summary>
    private sealed class RestoredFinanceOperationInsertRow
    {
        public int Result { get; set; }
        public int OperationId { get; set; }
    }

    /// <inheritdoc />
    public async Task<RestoredFinanceOperationInsert> InsertLostPayment(LostPaymentRow payment)
    {
        // Всё, что можно проверить до записи, проверяется до неё: ошибка должна называть параметр,
        // а не приходить из SQL. Тот же порядок, что у InsertRestoredFinanceOperation.
        if (payment.OperationType is not (FinanceOperationType.Online or FinanceOperationType.Refund))
        {
            throw new ArgumentOutOfRangeException(nameof(payment), payment.OperationType, "Восстанавливаются только онлайн-платёж и его возврат");
        }

        var isPayment = payment.OperationType == FinanceOperationType.Online;
        if (isPayment && (payment.OperationId is null || payment.RefundedOperationId is not null))
        {
            throw new ArgumentException("Платежу нужен прежний номер операции и не нужен номер возвращаемой", nameof(payment));
        }

        if (!isPayment && (payment.OperationId is not null || payment.RefundedOperationId is null))
        {
            throw new ArgumentException("Возврату нужен номер возвращаемой операции и не нужен прежний номер", nameof(payment));
        }

        if (isPayment ? payment.MoneyAmount <= 0 : payment.MoneyAmount >= 0)
        {
            throw new ArgumentException("Сумма платежа положительна, сумма возврата отрицательна", nameof(payment));
        }

        if (!isPayment && string.IsNullOrWhiteSpace(payment.BankRefundKey))
        {
            throw new ArgumentException("Возврату нужен номер возврата в банке", nameof(payment));
        }

        if (payment.PaymentTypeId.ProjectId != payment.ClaimId.ProjectId)
        {
            throw new ArgumentException($"Тип оплаты {payment.PaymentTypeId} не из проекта заявки {payment.ClaimId}", nameof(payment));
        }

        if (!await ClaimExists(payment.ClaimId))
        {
            throw new JoinRpgEntityNotFoundException(payment.ClaimId.ClaimId, "claim");
        }

        // Каркас тот же, что у InsertRestoredFinanceOperation: один батч своей транзакцией, проверка
        // занятости номера с блокировкой диапазона, IDENTITY_INSERT выключается и при ошибке, всё
        // значением — параметрами. Отличия: вид операции Online/Refund, ExtraAction пустой (у боевого
        // онлайн-платежа его нет, а PaidFee означал бы взнос, принятый мастером руками) и строка
        // банковских реквизитов. Возврат своего номера в банке не имеет, поэтому вставляется с новым
        // и ссылается на исходную операцию; его повтор узнаётся по проведённому возврату той же
        // операции — тот же критерий, по которому боевой RefundAsync отказывает во втором возврате.
        var sql = $"""
            SET XACT_ABORT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                DECLARE @createdAt datetime = @p6;
                DECLARE @discussionId int;
                SELECT @discussionId = CommentDiscussionId FROM dbo.Claims WHERE ClaimId = @p0 AND ProjectId = @p1;
                IF @discussionId IS NULL
                    THROW 50000, N'Claim is not found in the project', 1;
                IF NOT EXISTS (SELECT 1 FROM dbo.PaymentTypes WHERE PaymentTypeId = @p10 AND ProjectId = @p1)
                    THROW 50000, N'Payment type is not found in the project', 1;
                DECLARE @operationId int;
                IF @p15 = 1
                BEGIN
                    DECLARE @commentId int = @p2;
                    DECLARE @existingDiscussionId int;
                    SELECT @existingDiscussionId = CommentDiscussionId FROM dbo.Comments WITH (UPDLOCK, HOLDLOCK) WHERE CommentId = @commentId;
                    IF @existingDiscussionId IS NOT NULL
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT CASE
                                WHEN @existingDiscussionId = @discussionId AND EXISTS (SELECT 1 FROM dbo.FinanceOperations WHERE CommentId = @commentId)
                                THEN {(int)RestoredCommentInsertResult.AlreadyExists}
                                ELSE {(int)RestoredCommentInsertResult.TakenElsewhere}
                            END AS Result,
                            @commentId AS OperationId;
                        RETURN;
                    END;
                    IF @commentId <= 0 OR @commentId >= ISNULL(IDENT_CURRENT('dbo.Comments'), 0)
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT {(int)RestoredCommentInsertResult.NotLostId} AS Result, @commentId AS OperationId;
                        RETURN;
                    END;
                    SET IDENTITY_INSERT dbo.Comments ON;
                    INSERT INTO dbo.Comments (CommentId, ProjectId, CommentDiscussionId, ParentCommentId, CreatedTime,
                        LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer, ExtraAction)
                    VALUES (@commentId, @p1, @discussionId, NULL, @createdAt, @createdAt, @p4, @p5, 1, NULL);
                    SET IDENTITY_INSERT dbo.Comments OFF;
                    SET @operationId = @commentId;
                END
                ELSE
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM dbo.FinanceOperations WHERE CommentId = @p3)
                        THROW 50000, N'Source payment operation is not found', 1;
                    DECLARE @existingRefundId int;
                    SELECT TOP 1 @existingRefundId = CommentId FROM dbo.FinanceOperations WITH (UPDLOCK, HOLDLOCK)
                    WHERE RefundedOperationId = @p3
                        AND OperationType = {(int)FinanceOperationType.Refund}
                        AND State = {(int)FinanceOperationState.Approved};
                    IF @existingRefundId IS NOT NULL
                    BEGIN
                        COMMIT TRANSACTION;
                        SELECT {(int)RestoredCommentInsertResult.AlreadyExists} AS Result, @existingRefundId AS OperationId;
                        RETURN;
                    END;
                    INSERT INTO dbo.Comments (ProjectId, CommentDiscussionId, ParentCommentId, CreatedTime,
                        LastEditTime, AuthorUserId, IsCommentByPlayer, IsVisibleToPlayer, ExtraAction)
                    VALUES (@p1, @discussionId, NULL, @createdAt, @createdAt, @p4, @p5, 1, NULL);
                    SET @operationId = CAST(SCOPE_IDENTITY() AS int);
                END;
                INSERT INTO dbo.CommentTexts (CommentId, Text_Contents) VALUES (@operationId, @p12);
                INSERT INTO dbo.FinanceOperations (CommentId, ProjectId, ClaimId, MoneyAmount, PaymentTypeId, Created,
                    Changed, OperationDate, State, OperationType, RefundedOperationId)
                VALUES (@operationId, @p1, @p0, @p9, @p10, @createdAt, @p7, @p8,
                    {(int)FinanceOperationState.Approved}, @p11, @p3);
                INSERT INTO dbo.FinanceOperationBankDetails (CommentId, BankOperationKey, BankRefundKey)
                VALUES (@operationId, @p13, @p14);
                COMMIT TRANSACTION;
                SELECT {(int)RestoredCommentInsertResult.Inserted} AS Result, @operationId AS OperationId;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                SET IDENTITY_INSERT dbo.Comments OFF;
                THROW;
            END CATCH;
            """;

        var row = await ctx.Database.SqlQuery<RestoredFinanceOperationInsertRow>(
                sql,
                payment.ClaimId.ClaimId,
                payment.ClaimId.ProjectId.Value,
                (object?)payment.OperationId ?? DBNull.Value,
                (object?)payment.RefundedOperationId ?? DBNull.Value,
                payment.AuthorId.Value,
                payment.IsCommentByPlayer,
                payment.CreatedAt,
                payment.ChangedAt,
                payment.OperationDate,
                payment.MoneyAmount,
                payment.PaymentTypeId.PaymentTypeId,
                (int)payment.OperationType,
                payment.Text,
                payment.BankOperationKey,
                (object?)payment.BankRefundKey ?? DBNull.Value,
                isPayment)
            .SingleAsync();
        return new RestoredFinanceOperationInsert((RestoredCommentInsertResult)row.Result, row.OperationId);
    }

    /// <summary>Пустой набор полей — так его сериализует сохранение полей (<c>SerializeFields</c>).</summary>
    private const string EmptyFieldsJson = "{}";

    private async Task<(Project, ProjectInfo)> LoadProject(ProjectIdentification projectId)
    {
        var project = await ProjectLoaderCommon.GetProjectWithFieldsAsync(ctx, projectId.Value, skipCache: false)
            ?? throw new ProjectNotFoundException(projectId);

        // Единственный источник истины Project -> ProjectInfo, тот же, что у read-пути.
        return (project, ProjectMetadataRepository.CreateInfoFromProject(project, projectId));
    }

    private async Task<Character> LoadCharacterEntity(CharacterIdentification characterId)
        => await CharacterQuery(ctx).SingleOrDefaultAsync(
                c => c.CharacterId == characterId.CharacterId && c.ProjectId == characterId.ProjectId.Value)
            ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");

    private async Task<Claim> LoadClaimEntity(ClaimIdentification claimId)
        => await ClaimQuery(ctx).SingleOrDefaultAsync(
                c => c.ClaimId == claimId.ClaimId && c.ProjectId == claimId.ProjectId.Value)
            ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

    private async Task<CharacterInfo> LoadCharacterInfo(CharacterIdentification characterId, ProjectInfo projectInfo)
    {
        // Грузим через загрузчик с ЯВНЫМ ProjectInfo: конструктор CharacterInfo требует, чтобы это
        // был ровно тот же экземпляр, что у хэндла (ADR013).
        var characterIntId = characterId.CharacterId;
        var infos = await loader.LoadAsync(projectInfo, character => character.CharacterId == characterIntId);
        return infos.SingleOrDefault()
            ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");
    }

    /// <summary>
    /// Связи персонажа, нужные бизнес-логике, грузятся явно, а не ленивой загрузкой.
    /// </summary>
    private static IQueryable<Character> CharacterQuery(MyDbContext ctx)
        => ctx.Set<Character>()
            .Include(c => c.Project)
            .Include(c => c.Project.ProjectAcls)
            .Include(c => c.Claims);

    /// <summary>
    /// Связи заявки, нужные бизнес-логике, грузятся явно, а не ленивой загрузкой
    /// (ср. <c>ClaimsRepositoryImpl.GetClaimImpl</c>, который часть из них тянет лениво).
    /// </summary>
    /// <remarks>
    /// <para><c>Player.Claims</c> — иначе <c>OtherActiveClaimsForThisPlayer</c> вернёт пусто
    /// и автоотклонение при <c>StrictlyOneCharacter</c> тихо исчезнет.</para>
    /// <para><c>CommentDiscussion.Comments</c> — иначе не найти родительский комментарий
    /// и сломается финансовая модерация.</para>
    /// </remarks>
    private static IQueryable<Claim> ClaimQuery(MyDbContext ctx)
        => ctx.Set<Claim>()
            .Include(c => c.Project)
            .Include(c => c.Project.ProjectAcls)
            .Include(c => c.Character)
            .Include(c => c.Player)
            .Include(c => c.Player.Claims)
            .Include(c => c.CommentDiscussion.Comments)
            .Include(c => c.AccommodationRequest)
            // Группа проживающих с составом — «ручки» для записи (ADR022 §4): приём приглашения
            // переселяет заявки из состава трекаемой группы принимающего, не загружая каждую
            // отдельно.
            .Include(c => c.AccommodationRequest!.Subjects)
            .Include(c => c.FinanceOperations);

    private class CharacterAggregateUpdateHandle(
        MyDbContext ctx,
        CharacterInfoLoader loader,
        Project project,
        ProjectInfo projectInfo,
        Character character,
        CharacterInfo characterInfo)
        : ICharacterAggregateUpdateHandle
    {
        public Project Project { get; } = project;

        public ProjectInfo ProjectInfo { get; } = projectInfo;

        public Character Character { get; } = character;

        public CharacterInfo CharacterInfo { get; } = characterInfo;

        public void Add(object entity) => _ = ctx.Set(entity.GetType()).Add(entity);

        public void Remove(object entity) => _ = ctx.Set(entity.GetType()).Remove(entity);

        public async Task<Claim> LoadOtherClaim(ClaimIdentification claimId)
            => await ClaimQuery(ctx).SingleOrDefaultAsync(
                    c => c.ClaimId == claimId.ClaimId && c.ProjectId == claimId.ProjectId.Value)
                ?? throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");

        public async Task<(Character Entity, CharacterInfo Info)> LoadOtherCharacter(CharacterIdentification characterId)
        {
            var entity = await CharacterQuery(ctx).SingleOrDefaultAsync(
                    c => c.CharacterId == characterId.CharacterId && c.ProjectId == characterId.ProjectId.Value)
                ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");

            var characterIntId = characterId.CharacterId;
            var infos = await loader.LoadAsync(ProjectInfo, c => c.CharacterId == characterIntId);
            var info = infos.SingleOrDefault()
                ?? throw new JoinRpgEntityNotFoundException(characterId.CharacterId, "character");

            return (entity, info);
        }

        /// <summary>
        /// Повторяет запрос <c>PlotRepositoryImpl.GetDirectPlotsForCharacter</c> один в один, но
        /// на <c>DbContext</c> этого хэндла — иначе добавление персонажа в
        /// <c>PlotElement.TargetCharacters</c> трекалось бы в чужом контексте и не сохранилось.
        /// </summary>
        public async Task<IReadOnlyCollection<PlotElement>> LoadDirectPlotsForCharacter(CharacterIdentification characterId)
        {
            var characterIntId = characterId.CharacterId;
            return await ctx.Set<PlotElement>()
                .Include(e => e.Texts)
                .Include(e => e.TargetCharacters)
                .Include(e => e.TargetGroups)
                .Where(e => e.TargetCharacters.Any(ch => ch.CharacterId == characterIntId))
                .ToListAsync();
        }

        /// <summary>
        /// Повторяет запрос, который снятие приглашений делало на собственном <c>DbContext</c>, но
        /// на контексте этого хэндла. Заявки-участницы (<c>From</c>/<c>To</c>) грузятся сразу:
        /// вызывающие читают их, чтобы понять, чьи приглашения затронуты.
        /// </summary>
        public async Task<IReadOnlyCollection<AccommodationInvite>> LoadInvitesForClaim(ClaimIdentification claimId)
        {
            var claimIntId = claimId.ClaimId;
            var projectIntId = claimId.ProjectId.Value;
            return await ctx.Set<AccommodationInvite>()
                .Include(i => i.From)
                .Include(i => i.To)
                .Where(i => i.ProjectId == projectIntId)
                .Where(i => i.ToClaimId == claimIntId || i.FromClaimId == claimIntId)
                .ToListAsync();
        }

        public async Task<AccommodationRequest?> LoadAccommodationGroup(AccommodationRequestIdentification groupId)
        {
            var groupIntId = groupId.AccommodationRequestId;
            return await AccommodationGroupQuery(groupId.ProjectId.Value)
                .SingleOrDefaultAsync(request => request.Id == groupIntId);
        }

        /// <summary>
        /// То же ядро, что у <see cref="RoomCategoryPlanRepository"/>, но на <see cref="ProjectInfo"/>
        /// хэндла: иначе конструктор плана не признал бы типы проживания своими (ADR013). Запрос
        /// без трекинга, поэтому трекаемые сущности мутации он не задевает.
        /// </summary>
        public async Task<RoomCategoryPlan> LoadRoomCategoryPlan(AccommodationTypeIdentification typeId)
        {
            // Категорию по типу знают только метаданные (ADR018, §2).
            var type = ProjectInfo.AccommodationSettings.GetTypeById(typeId);
            return await new RoomCategoryPlanLoader(ctx).LoadOneAsync(ProjectInfo, type.RoomCategoryId)
                ?? throw new InvalidOperationException(
                    $"Room category {type.RoomCategoryId} of accommodation type {typeId} is not found");
        }

        public async Task<AccommodationInvite> LoadInvite(AccommodationInviteIdentification inviteId)
        {
            var inviteIntId = inviteId.AccommodationInviteId;
            var projectIntId = inviteId.ProjectId.Value;
            return await ctx.Set<AccommodationInvite>()
                    .Include(invite => invite.From)
                    .Include(invite => invite.To)
                    .SingleOrDefaultAsync(invite => invite.Id == inviteIntId && invite.ProjectId == projectIntId)
                ?? throw new JoinRpgEntityNotFoundException(inviteIntId, nameof(AccommodationInvite));
        }

        /// <summary>
        /// Строка группы без связей. Решения о составе и свободном месте принимаются по плану
        /// поселения (ADR022 §4), а от трекаемой группы приглашению нужны только её существование
        /// и тип — поэтому ни состав, ни жильцы комнаты здесь не грузятся.
        /// </summary>
        private IQueryable<AccommodationRequest> AccommodationGroupQuery(int projectId)
            => ctx.Set<AccommodationRequest>()
                .Where(request => request.ProjectId == projectId);
    }

    private sealed class ClaimUpdateHandle(
        MyDbContext ctx,
        CharacterInfoLoader loader,
        Project project,
        ProjectInfo projectInfo,
        Character character,
        CharacterInfo characterInfo,
        Claim claim,
        CharacterClaimInfo claimInfo)
        : CharacterAggregateUpdateHandle(ctx, loader, project, projectInfo, character, characterInfo),
            IClaimUpdateHandle
    {
        public Claim Claim { get; } = claim;

        public ClaimInCharacter ClaimSnapshot { get; } = new(characterInfo, claimInfo);
    }
}
