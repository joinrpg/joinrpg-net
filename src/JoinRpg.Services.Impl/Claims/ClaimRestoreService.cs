using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Projects;
using JoinRpg.Services.Interfaces.Drp;

namespace JoinRpg.Services.Impl.Claims;

/// <inheritdoc cref="IClaimRestoreService"/>
/// <remarks>
/// <para>
/// Восстановление повторяет уже случившееся, поэтому правила новых решений мастера (таблица переходов
/// статуса, проверки переноса и смены ответственного) здесь не применяются, а каскадов операций
/// (автоотклонение других заявок, выселение, приглашения) нет: у затронутых ими заявок свои события
/// в уведомлениях, и они восстанавливаются отдельно.
/// </para>
/// <para>
/// Решения принимаются по снимкам до изменения (ADR021), и только потом мутируются сущности: если
/// менять нечего или нельзя, отслеживаемые сущности остаются нетронутыми.
/// </para>
/// <para>
/// Запись в истории ставит восстановителя последним комментировавшим мастером заявки
/// (<c>LastMasterCommentAt/By</c>) — видимых игроку отметок она не трогает. Даты статуса, который
/// совпал с прежним, не меняются. Вернуть взнос «по умолчанию» (null) нельзя: null значит «не менять».
/// </para>
/// </remarks>
internal class ClaimRestoreService(
    ICharacterPropsService characterPropsService,
    IUnitOfWork unitOfWork,
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor) : IClaimRestoreService
{
    public Task ApplyRestoredState(ClaimIdentification claimId, RestoredClaimTarget target)
        => ApplyCore(claimId, target, isCreation: false);

    /// <remarks>
    /// Три шага без общей транзакции: у сервиса и у <see cref="ICharacterPropsService"/> разные
    /// <c>DbContext</c> (<c>MyDbContext</c> транзиентный). Вместо неё — повторяемость: оборвись на
    /// любом шаге, повторный предпросмотр найдёт созданного персонажа по имени, а вставленную оболочку
    /// предложит «изменить».
    /// </remarks>
    public async Task<CharacterIdentification> RestoreLostClaim(LostClaimToRestore request)
    {
        // Всё, что можно проверить, — до первой записи: после неё неудача оставит персонажа или оболочку.
        var projectId = request.ClaimId.ProjectId;
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        EnsureProjectBlocked(projectInfo);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims);
        _ = EnsureMaster(projectInfo, request.ResponsibleMaster);
        if (request.Target.ResponsibleMaster is { } targetMaster)
        {
            _ = EnsureMaster(projectInfo, targetMaster);
        }
        _ = await unitOfWork.GetUsersRepository().GetRequiredUserInfoHeader(request.Player);

        var now = DateTime.UtcNow;
        EnsureRestoredDate(request.CreatedAt, now);
        if (request.Target.StatusChangedAt is { } statusChangedAt)
        {
            EnsureRestoredDate(statusChangedAt, now);
        }

        if (request.Target.Character is not null)
        {
            // Персонаж новой заявки задаётся только Character/NewCharacterName — иначе их два.
            throw new ArgumentException("Персонаж новой заявки задаётся только Character или NewCharacterName", nameof(request));
        }
        if (request.Character is not null && request.NewCharacterIsSlot)
        {
            throw new ArgumentException("Шаблоном создаётся только новый персонаж", nameof(request));
        }

        var writeRepository = unitOfWork.GetCharacterAggregateWriteRepository();
        if (await writeRepository.IsClaimIdTaken(request.ClaimId.ClaimId))
        {
            // TODO[Localize]
            throw new JoinValidationException($"Заявка с номером {request.ClaimId.ClaimId} уже есть в базе.");
        }

        if (request.Character is { } existingCharacter)
        {
            if (existingCharacter.ProjectId != projectId)
            {
                throw new ArgumentException($"Персонаж {existingCharacter} из другого проекта", nameof(request));
            }
            // Загрузка агрегата заодно проверяет, что персонаж в этом проекте есть.
            var handle = await writeRepository.LoadCharacterForUpdate(existingCharacter);
            if (request.Target.Status is ClaimStatus.Approved or ClaimStatus.CheckedIn)
            {
                EnsureCanBeApprovedOn(handle.CharacterInfo, request.ClaimId);
            }
        }
        else if (string.IsNullOrWhiteSpace(request.NewCharacterName))
        {
            // TODO[Localize]
            throw new JoinValidationException("Не задано имя персонажа.");
        }

        var characterId = request.Character
            ?? await CreateCharacter(
                projectId,
                request.NewCharacterName!,
                request.NewCharacterIsSlot ? CharacterType.Slot : CharacterType.Player,
                request.NewCharacterVisibility);

        var inserted = await unitOfWork.GetCharacterAggregateWriteRepository().InsertRestoredClaimShell(new RestoredClaimShell(
            request.ClaimId, characterId, request.Player, request.ResponsibleMaster, request.CreatedAt.UtcDateTime));
        if (!inserted)
        {
            // TODO[Localize]
            throw new JoinValidationException($"Заявка {request.ClaimId.ClaimId} уже есть в базе.");
        }

        await ApplyCore(request.ClaimId, request.Target, isCreation: true);
        return characterId;
    }

    /// <remarks>
    /// <para>
    /// Каждый комментарий вставляется своим батчем, а отметки заявки ставит потом операция над
    /// заявкой; общей транзакции нет (см. <see cref="RestoreLostClaim"/>). Вместо неё — повторяемость:
    /// оборвись посередине, повтор узнает вставленные комментарии и всё равно подтянет отметки заявки
    /// по всем переданным, но запись в истории напишет, только если что-то вставил сам.
    /// </para>
    /// <para>
    /// Отметки о последних комментариях — максимум из прежних и восстановленных. Запись в истории
    /// ставит восстановителя последним комментировавшим мастером «сейчас»; здесь это сломало бы фильтр
    /// «без ответа», поэтому мастерские отметки после неё возвращаются к вычисленным. Дату обновления
    /// заявки ставит текущей: сами комментарии несут исторические даты, а дату обновления проекта надо
    /// поднять (ADR023).
    /// </para>
    /// </remarks>
    public async Task<RestoredCommentsResult> RestoreClaimComments(ClaimIdentification claimId, IReadOnlyList<RestoredClaimComment> comments)
    {
        // Всё, что можно проверить, — до первой записи.
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);
        EnsureProjectBlocked(projectInfo);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims);

        var now = DateTime.UtcNow;
        foreach (var comment in comments)
        {
            EnsureRestoredDate(comment.CreatedAt, now);
            if (comment.IsCommentByPlayer && !comment.IsVisibleToPlayer)
            {
                throw new ArgumentException($"Комментарий игрока {comment.CommentId} не может быть скрыт от игрока", nameof(comments));
            }
            // Как Comment.Validate: пустым бывает только комментарий с действием.
            if (comment.ExtraAction is null && string.IsNullOrWhiteSpace(comment.Text))
            {
                throw new ArgumentException($"Пустой комментарий {comment.CommentId} без действия", nameof(comments));
            }
        }
        if (comments.Where(c => c.CommentId is not null).GroupBy(c => c.CommentId).FirstOrDefault(g => g.Count() > 1) is { } duplicate)
        {
            throw new ArgumentException($"Комментарий {duplicate.Key} передан дважды", nameof(comments));
        }
        _ = await unitOfWork.GetUsersRepository().GetRequiredUserInfoHeaders([.. comments.Select(c => c.Author).Distinct()]);

        var writeRepository = unitOfWork.GetCharacterAggregateWriteRepository();
        if (!await writeRepository.ClaimExists(claimId))
        {
            throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");
        }

        if (comments.Count == 0)
        {
            return new RestoredCommentsResult(0, 0, []);
        }

        var present = new List<RestoredClaimComment>();
        var inserted = 0;
        var conflicts = new List<int>();
        foreach (var comment in comments)
        {
            var result = await writeRepository.InsertRestoredClaimComment(new RestoredClaimCommentRow(
                claimId,
                comment.CommentId,
                comment.Author,
                comment.IsCommentByPlayer,
                comment.IsVisibleToPlayer,
                comment.ExtraAction,
                comment.CreatedAt.UtcDateTime,
                comment.Text));
            switch (result)
            {
                case RestoredCommentInsertResult.Inserted:
                    inserted++;
                    present.Add(comment);
                    break;
                case RestoredCommentInsertResult.AlreadyExists:
                    present.Add(comment);
                    break;
                case RestoredCommentInsertResult.TakenElsewhere:
                case RestoredCommentInsertResult.NotLostId:
                    conflicts.Add(comment.CommentId!.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(comments), result, null);
            }
        }

        if (present.Count > 0)
        {
            await ApplyRestoredComments(claimId, new RestoredCommentsToApply(present, inserted));
        }

        return new RestoredCommentsResult(inserted, present.Count - inserted, conflicts);
    }

    /// <param name="Comments">Комментарии, которые теперь есть в обсуждении, — вставленные и уже бывшие.</param>
    /// <param name="Inserted">Сколько из них вставлено этим вызовом.</param>
    private sealed record RestoredCommentsToApply(IReadOnlyList<RestoredClaimComment> Comments, int Inserted);

    private Task ApplyRestoredComments(ClaimIdentification claimId, RestoredCommentsToApply request)
        => characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.ManageClaims,
            ProjectActiveRequirement.AllowBlocked,
            request,
            ctx =>
            {
                EnsureProjectBlocked(ctx.ProjectInfo);
                var claim = ctx.Claim;

                // 1. Решения — по значениям до операции.
                var marksBefore = CommentMarks.Of(claim);
                var marks = marksBefore.With(ctx.Request.Comments.Select(
                    c => new CommentMark(c.Author, c.IsCommentByPlayer, c.IsVisibleToPlayer, c.CreatedAt)));

                if (ctx.Request.Inserted == 0 && marks == marksBefore)
                {
                    ctx.NothingChanged();
                    return;
                }

                // 2. Мутации.
                if (ctx.Request.Inserted > 0)
                {
                    // TODO[Localize]
                    _ = ctx.AddHistoryRecordWithoutNotification(
                        $"Восстановлено комментариев из уведомлений: {ctx.Request.Inserted}",
                        CommentExtraAction.RestoredFromNotifications);
                }

                // После записи в истории: она поставила восстановителя последним мастером «сейчас».
                marks.ApplyTo(claim);
                claim.LastUpdateDateTime = ctx.Now;
            });

    /// <summary>Восстановленный комментарий глазами отметок заявки о последних комментариях.</summary>
    private sealed record CommentMark(UserIdentification Author, bool IsCommentByPlayer, bool IsVisibleToPlayer, DateTimeOffset CreatedAt);

    /// <summary>
    /// Отметки заявки о последних комментариях. Восстановленные комментарии подтягивают каждую к самому
    /// позднему из своих, если он позже прежней, — как если бы комментарии писались в своё время.
    /// </summary>
    private sealed record CommentMarks(
        DateTimeOffset? PlayerAt,
        DateTimeOffset? MasterAt,
        int? MasterBy,
        DateTimeOffset? VisibleAt,
        int? VisibleBy)
    {
        public static CommentMarks Of(Claim claim) => new(
            claim.LastPlayerCommentAt,
            claim.LastMasterCommentAt,
            claim.LastMasterCommentBy_Id,
            claim.LastVisibleMasterCommentAt,
            claim.LastVisibleMasterCommentBy_Id);

        public CommentMarks With(IEnumerable<CommentMark> restored)
        {
            var comments = restored.ToList();
            var playerAt = Later(
                PlayerAt,
                comments.Where(c => c.IsCommentByPlayer).Select(c => (DateTimeOffset?)c.CreatedAt.ToUniversalTime()).Max());
            var (masterAt, masterBy) = LatestMaster(MasterAt, MasterBy, comments.Where(c => !c.IsCommentByPlayer));
            var (visibleAt, visibleBy) = LatestMaster(
                VisibleAt, VisibleBy, comments.Where(c => !c.IsCommentByPlayer && c.IsVisibleToPlayer));
            return new(playerAt, masterAt, masterBy, visibleAt, visibleBy);
        }

        public void ApplyTo(Claim claim)
        {
            claim.LastPlayerCommentAt = PlayerAt;
            claim.LastMasterCommentAt = MasterAt;
            claim.LastMasterCommentBy_Id = MasterBy;
            claim.LastVisibleMasterCommentAt = VisibleAt;
            claim.LastVisibleMasterCommentBy_Id = VisibleBy;
        }

        private static DateTimeOffset? Later(DateTimeOffset? before, DateTimeOffset? restored)
            => restored is { } r && (before is null || r > before) ? r : before;

        /// <summary>Отметка «последний мастерский комментарий»: прежняя либо самый поздний восстановленный, если он позже.</summary>
        private static (DateTimeOffset? At, int? By) LatestMaster(DateTimeOffset? beforeAt, int? beforeBy, IEnumerable<CommentMark> comments)
        {
            var latest = comments.MaxBy(c => c.CreatedAt);
            return latest is not null && (beforeAt is null || latest.CreatedAt > beforeAt)
                ? (latest.CreatedAt.ToUniversalTime(), latest.Author.Value)
                : (beforeAt, beforeBy);
        }
    }

    /// <remarks>
    /// <para>
    /// Как <see cref="RestoreClaimComments"/>: каждая операция и каждое решение вставляются своим батчем,
    /// а взнос, отметки и состояние уже бывших операций ставит потом одна операция над заявкой; общей
    /// транзакции нет, вместо неё — повторяемость. Оборвись посередине, повтор узнает вставленное и
    /// доделает остальное, но запись в истории напишет, только если что-то вставил сам.
    /// </para>
    /// <para>
    /// Права — только <see cref="Permission.CanManageMoney"/>: ни одна доменная финансовая операция
    /// права на заявки не требует, а возврат и подтверждение чужих платежей требуют права на деньги.
    /// </para>
    /// <para>
    /// Решение по заявленному платежу, который был в базе до вызова и ждёт решения, переводит его в
    /// итоговое состояние временем решения — как <c>ModerateFinanceOperation</c>. Сюда же попадает
    /// операция, которую прошлый прогон восстановил ещё без решения. У операции, вставленной этим
    /// вызовом, состояние уже итоговое (и совпадает с решением), решение его не трогает. Онлайн-операции
    /// ждут банк, а у запроса льготного взноса восстанавливается только комментарий: что он решил
    /// (льготник ли игрок), восстанавливается отдельно. Решение без родителя (не связанное с
    /// операцией) вставляется комментарием без родителя и состояния не меняет.
    /// </para>
    /// <para>
    /// Взнос фиксируется так же, как при обычной оплате (<c>UpdateClaimFeeIfRequired</c>), но по
    /// одобренным суммам в порядке дат операций: снимок заявки читается уже после вставки, поэтому
    /// восстановленные суммы в нём есть, и каждая проверка «оплачено ли полностью» видит только
    /// оплаченное к своей дате.
    /// </para>
    /// </remarks>
    public async Task<RestoredFinanceResult> RestoreFinanceOperations(
        ClaimIdentification claimId,
        IReadOnlyList<RestoredFinanceOperation> operations,
        IReadOnlyList<RestoredFinanceModeration> moderations)
    {
        // Всё, что можно проверить, — до первой записи.
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);
        EnsureProjectBlocked(projectInfo);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageMoney);

        var now = DateTime.UtcNow;
        foreach (var operation in operations)
        {
            EnsureRestoredOperation(projectInfo, operation, now);
        }

        var ids = operations.Select(o => o.OperationId).Concat(moderations.Select(m => m.CommentId)).Where(id => id is not null);
        if (ids.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1) is { } duplicate)
        {
            throw new ArgumentException($"Идентификатор {duplicate.Key} передан дважды", nameof(operations));
        }

        _ = await unitOfWork.GetUsersRepository().GetRequiredUserInfoHeaders(
            [.. operations.Select(o => o.Author).Concat(moderations.Select(m => m.Author)).Distinct()]);

        var writeRepository = unitOfWork.GetCharacterAggregateWriteRepository();
        if (!await writeRepository.ClaimExists(claimId))
        {
            throw new JoinRpgEntityNotFoundException(claimId.ClaimId, "claim");
        }

        var restoredIndexById = operations
            .Select((operation, index) => (operation.OperationId, index))
            .Where(x => x.OperationId is not null)
            .ToDictionary(x => x.OperationId!.Value, x => x.index);
        if (moderations.Count > 0)
        {
            // Операции заявки, которые уже есть в базе, — родители решений; загрузка агрегата заодно проверяет заявку.
            var handle = await writeRepository.LoadClaimForUpdate(claimId);
            var existingTypes = handle.Claim.FinanceOperations.ToDictionary(f => f.CommentId, f => f.OperationType);
            foreach (var moderation in moderations)
            {
                EnsureRestoredModeration(operations, restoredIndexById, existingTypes, moderation, now);
            }
        }

        // Вставка.
        var operationEntries = new List<RestoredFinanceEntry>();
        foreach (var operation in operations)
        {
            var insert = await writeRepository.InsertRestoredFinanceOperation(new RestoredFinanceOperationRow(
                claimId,
                operation.OperationId,
                operation.Author,
                operation.IsCommentByPlayer,
                operation.Type,
                operation.Money,
                operation.PaymentType,
                operation.State,
                operation.CreatedAt.UtcDateTime,
                operation.ChangedAt.UtcDateTime,
                OperationDateOf(projectInfo, operation.CreatedAt),
                operation.Text));
            operationEntries.Add(new RestoredFinanceEntry(ToEntryStatus(insert.Result), insert.OperationId));
        }

        var moderationEntries = new List<RestoredFinanceEntry>();
        var presentModerations = new List<PresentModeration>();
        foreach (var moderation in moderations)
        {
            // Без родителя — решение, которое не удалось связать с операцией: вставляется само по себе.
            var hasParent = moderation.ParentOperationId is not null || moderation.ParentRestoredIndex is not null;
            var restoredIndex = moderation.ParentRestoredIndex
                ?? (moderation.ParentOperationId is { } parentOperationId && restoredIndexById.TryGetValue(parentOperationId, out var index)
                    ? index
                    : null);
            int? parent = restoredIndex is { } i
                ? (operationEntries[i].IsPresent ? operationEntries[i].Id : null)
                : moderation.ParentOperationId;
            if (hasParent && parent is null)
            {
                moderationEntries.Add(new RestoredFinanceEntry(RestoredFinanceEntryStatus.ParentNotRestored, moderation.CommentId));
                continue;
            }

            var result = await writeRepository.InsertRestoredClaimComment(new RestoredClaimCommentRow(
                claimId,
                moderation.CommentId,
                moderation.Author,
                IsCommentByPlayer: false,
                IsVisibleToPlayer: true,
                moderation.Approved ? CommentExtraAction.ApproveFinance : CommentExtraAction.RejectFinance,
                moderation.CreatedAt.UtcDateTime,
                moderation.Text,
                parent));
            var entry = new RestoredFinanceEntry(ToEntryStatus(result), moderation.CommentId);
            moderationEntries.Add(entry);
            if (entry.IsPresent)
            {
                // «Восстановленный» родитель — только вставленный этим вызовом: уже бывший мог прийти в
                // прошлый раз в другом состоянии (заявлен без решения), и решение должно его перевести.
                presentModerations.Add(new PresentModeration(moderation, parent,
                    ParentInserted: restoredIndex is { } r && operationEntries[r].Status == RestoredFinanceEntryStatus.Inserted));
            }
        }

        var presentOperations = operations
            .Zip(operationEntries)
            .Where(x => x.Second.IsPresent)
            .Select(x => new PresentOperation(x.Second.Id!.Value, x.First))
            .ToList();
        if (presentOperations.Count > 0 || presentModerations.Count > 0)
        {
            await ApplyRestoredFinance(claimId, new RestoredFinanceToApply(
                presentOperations,
                presentModerations,
                InsertedOperations: operationEntries.Count(e => e.Status == RestoredFinanceEntryStatus.Inserted),
                InsertedModerations: moderationEntries.Count(e => e.Status == RestoredFinanceEntryStatus.Inserted)));
        }

        return new RestoredFinanceResult(operationEntries, moderationEntries);
    }

    /// <summary>Операция, которая теперь есть в заявке, — вставленная или уже бывшая.</summary>
    private sealed record PresentOperation(int Id, RestoredFinanceOperation Operation);

    /// <summary>Решение, которое теперь есть в заявке.</summary>
    /// <param name="ParentId">Операция-родитель; <c>null</c> — решение, не связанное с операцией.</param>
    /// <param name="ParentInserted">Родитель вставлен этим вызовом — его состояние уже итоговое.</param>
    private sealed record PresentModeration(RestoredFinanceModeration Moderation, int? ParentId, bool ParentInserted);

    private sealed record RestoredFinanceToApply(
        IReadOnlyList<PresentOperation> Operations,
        IReadOnlyList<PresentModeration> Moderations,
        int InsertedOperations,
        int InsertedModerations);

    private Task ApplyRestoredFinance(ClaimIdentification claimId, RestoredFinanceToApply request)
        => characterPropsService.ChangeClaim(
            claimId,
            ClaimAccessRequirement.ManageMoney,
            ProjectActiveRequirement.AllowBlocked,
            request,
            ctx =>
            {
                EnsureProjectBlocked(ctx.ProjectInfo);
                var claim = ctx.Claim;

                FinanceOperation Operation(int id)
                    => claim.FinanceOperations.SingleOrDefault(f => f.CommentId == id)
                        ?? throw new InvalidOperationException($"Операции {id} нет в заявке {claimId}");

                // 1. Решения — по значениям до операции.
                // Решения по операциям, которые уже были в базе и ждут решения, — только по заявленным
                // платежам (онлайн ждёт банк, льготу не трогаем); действует первое по времени.
                var transitions = new List<(FinanceOperation Operation, RestoredFinanceModeration Moderation)>();
                // Решение без родителя ничьё состояние не меняет.
                foreach (var moderation in ctx.Request.Moderations
                    .Where(m => !m.ParentInserted && m.ParentId is not null)
                    .OrderBy(m => m.Moderation.CreatedAt))
                {
                    var operation = Operation(moderation.ParentId!.Value);
                    if (operation.State == FinanceOperationState.Proposed
                        && operation.OperationType == FinanceOperationType.Submit
                        && transitions.All(t => t.Operation != operation))
                    {
                        transitions.Add((operation, moderation.Moderation));
                    }
                }

                var marksBefore = CommentMarks.Of(claim);
                var marks = marksBefore.With(
                    ctx.Request.Operations.Select(o => new CommentMark(o.Operation.Author, o.Operation.IsCommentByPlayer, IsVisibleToPlayer: true, o.Operation.CreatedAt))
                    .Concat(ctx.Request.Moderations.Select(m => new CommentMark(m.Moderation.Author, IsCommentByPlayer: false, IsVisibleToPlayer: true, m.Moderation.CreatedAt))));

                // Взнос. Состояние и сумма — из базы, а не из входа: «уже бывшая» операция могла найтись
                // по совпадению и отличаться от восстановленной. Одобренные восстановленные суммы уже есть
                // в снимке, а одобряемые этим вызовом бывшие — ещё нет. Distinct: две одинаковые операции
                // без id в пределах минуты узнаются как одна строка базы.
                var approvedRestored = ctx.Request.Operations
                    .Select(o => o.Id)
                    .Distinct()
                    .Select(Operation)
                    .Where(f => f.State == FinanceOperationState.Approved)
                    .ToList();
                var inSnapshot = approvedRestored.Sum(f => f.MoneyAmount);
                var feeBefore = claim.CurrentFee;
                var paid = 0;
                foreach (var operation in approvedRestored
                    .Concat(transitions.Where(t => t.Moderation.Approved).Select(t => t.Operation))
                    .OrderBy(f => f.OperationDate))
                {
                    paid += operation.MoneyAmount;
                    // Фиксирует взнос, только если он ещё не зафиксирован и оплачен полностью; иначе заявку не трогает.
                    claim.UpdateClaimFeeIfRequired(ctx.ClaimSnapshot, operation.OperationDate, paymentAdded: paid - inSnapshot);
                }

                if (ctx.Request.InsertedOperations == 0 && ctx.Request.InsertedModerations == 0
                    && transitions.Count == 0 && claim.CurrentFee == feeBefore && marks == marksBefore)
                {
                    ctx.NothingChanged();
                    return;
                }

                // 2. Мутации.
                foreach (var (operation, moderation) in transitions)
                {
                    operation.State = moderation.Approved ? FinanceOperationState.Approved : FinanceOperationState.Declined;
                    operation.Changed = moderation.CreatedAt.UtcDateTime;
                }

                if (ctx.Request.InsertedOperations > 0 || ctx.Request.InsertedModerations > 0)
                {
                    // TODO[Localize]
                    var text = $"Восстановлено финансовых операций из уведомлений: {ctx.Request.InsertedOperations}";
                    if (ctx.Request.InsertedModerations > 0)
                    {
                        text += $", решений по ним: {ctx.Request.InsertedModerations}";
                    }
                    _ = ctx.AddHistoryRecordWithoutNotification(text, CommentExtraAction.RestoredFromNotifications);
                }

                // После записи в истории: она поставила восстановителя последним мастером «сейчас».
                marks.ApplyTo(claim);
                claim.LastUpdateDateTime = ctx.Now;
            });

    /// <summary>
    /// Дата операции — как её пишет форма приёма взноса: полночь дня операции без часового пояса. День —
    /// в часовом поясе проекта: мастер выбирает в форме свой, местный день.
    /// </summary>
    private static DateTime OperationDateOf(ProjectInfo projectInfo, DateTimeOffset at)
        => TimeZoneInfo.ConvertTime(at, projectInfo.TimeZone).Date;

    private static void EnsureRestoredOperation(ProjectInfo projectInfo, RestoredFinanceOperation operation, DateTime now)
    {
        EnsureRestoredDate(operation.CreatedAt, now);
        EnsureRestoredDate(operation.ChangedAt, now);
        if (operation.ChangedAt < operation.CreatedAt)
        {
            throw new ArgumentException($"Операция {operation.OperationId} изменена раньше, чем создана", nameof(operation));
        }

        var signMatches = operation.Type switch
        {
            FinanceOperationType.Submit => operation.Money > 0,
            FinanceOperationType.Refund => operation.Money < 0,
            _ => throw new ArgumentException($"Восстанавливаются только взносы и возвраты, а не {operation.Type}", nameof(operation)),
        };
        if (!signMatches)
        {
            throw new ArgumentException($"Сумма {operation.Money} операции {operation.OperationId} не по знаку её вида {operation.Type}", nameof(operation));
        }

        if (operation.State is not (FinanceOperationState.Approved or FinanceOperationState.Proposed or FinanceOperationState.Declined))
        {
            throw new ArgumentOutOfRangeException(nameof(operation), operation.State, "Неизвестное состояние операции");
        }
        // Как у доменного возврата: его делает распорядитель денег, он сразу подтверждён.
        if (operation.Type == FinanceOperationType.Refund && operation.State != FinanceOperationState.Approved)
        {
            throw new ArgumentException($"Возврат {operation.OperationId} бывает только подтверждённым", nameof(operation));
        }

        // Выключенный тип допустим: операция была, когда он работал.
        var paymentType = operation.PaymentType.ProjectId == projectInfo.ProjectId
            ? projectInfo.ProjectFinanceSettings.GetPaymentByIdOrDefault(operation.PaymentType)
            : null;
        if (paymentType is null)
        {
            // TODO[Localize]
            throw new JoinValidationException($"Тип оплаты {operation.PaymentType.PaymentTypeId} не найден в проекте.");
        }
        if (paymentType.TypeKind.IsOnline())
        {
            throw new ArgumentException($"Онлайн-оплата {operation.OperationId} так не восстанавливается", nameof(operation));
        }
    }

    private static void EnsureRestoredModeration(
        IReadOnlyList<RestoredFinanceOperation> operations,
        Dictionary<int, int> restoredIndexById,
        Dictionary<int, FinanceOperationType> existingOperationTypes,
        RestoredFinanceModeration moderation,
        DateTime now)
    {
        EnsureRestoredDate(moderation.CreatedAt, now);

        RestoredFinanceOperation? restoredParent;
        switch (moderation)
        {
            case { ParentOperationId: null, ParentRestoredIndex: null }:
                // Не связанное с операцией решение — только комментарий.
                restoredParent = null;
                break;
            case { ParentOperationId: null, ParentRestoredIndex: { } index }:
                if (index < 0 || index >= operations.Count || operations[index].OperationId is not null)
                {
                    throw new ArgumentException(
                        $"Решение {moderation.CommentId}: по индексу {index} нет восстанавливаемой операции без id", nameof(moderation));
                }
                restoredParent = operations[index];
                break;
            case { ParentOperationId: { } parentId, ParentRestoredIndex: null }:
                restoredParent = restoredIndexById.TryGetValue(parentId, out var restoredIndex) ? operations[restoredIndex] : null;
                if (restoredParent is null)
                {
                    if (!existingOperationTypes.TryGetValue(parentId, out var type))
                    {
                        // TODO[Localize]
                        throw new JoinValidationException($"Финансовой операции {parentId} нет в заявке.");
                    }
                    // Решение мастера бывает только по заявленному игроком платежу или запросу льготы;
                    // онлайн-операции и возвраты ждут банк, а не мастера.
                    if (type is not (FinanceOperationType.Submit or FinanceOperationType.PreferentialFeeRequest))
                    {
                        // TODO[Localize]
                        throw new JoinValidationException($"Финансовая операция {parentId} не подлежит решению мастера.");
                    }
                }
                break;
            default:
                throw new ArgumentException($"Решение {moderation.CommentId}: операция задаётся не более чем одним способом", nameof(moderation));
        }

        if (restoredParent is not null)
        {
            if (moderation.CreatedAt < restoredParent.CreatedAt)
            {
                throw new ArgumentException($"Решение {moderation.CommentId} раньше своей операции", nameof(moderation));
            }
            // Состояние восстанавливаемой операции — итоговое, и решение обязано с ним совпадать.
            var decided = moderation.Approved ? FinanceOperationState.Approved : FinanceOperationState.Declined;
            if (restoredParent.State != decided)
            {
                throw new ArgumentException(
                    $"Решение {moderation.CommentId} не совпадает с состоянием {restoredParent.State} своей операции", nameof(moderation));
            }
        }
    }

    private static RestoredFinanceEntryStatus ToEntryStatus(RestoredCommentInsertResult result) => result switch
    {
        RestoredCommentInsertResult.Inserted => RestoredFinanceEntryStatus.Inserted,
        RestoredCommentInsertResult.AlreadyExists => RestoredFinanceEntryStatus.AlreadyExisted,
        RestoredCommentInsertResult.TakenElsewhere => RestoredFinanceEntryStatus.TakenElsewhere,
        RestoredCommentInsertResult.NotLostId => RestoredFinanceEntryStatus.NotLostId,
        _ => throw new ArgumentOutOfRangeException(nameof(result), result, null),
    };

    /// <summary>
    /// Персонаж игрока или шаблон в корневой группе: групп в уведомлениях нет, разложит мастер.
    /// Видимость — какую выбрал мастер: какой она была, неизвестно (#5677). Шаблон — без ограничения
    /// числа персонажей: сколько их было, тоже неизвестно.
    /// </summary>
    private async Task<CharacterIdentification> CreateCharacter(
        ProjectIdentification projectId,
        string? name,
        CharacterType characterType,
        CharacterVisibility visibility)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            // TODO[Localize]
            throw new JoinValidationException("Не задано имя персонажа.");
        }

        var character = await characterPropsService.CreateCharacter(
            projectId,
            Permission.CanEditRoles,
            ProjectActiveRequirement.AllowBlocked,
            name,
            ctx =>
            {
                var character = new Character
                {
                    ProjectId = ctx.ProjectInfo.ProjectId.Value,
                    Project = ctx.Project,
                };

                ctx.SetCharacterSettings(character, new CharacterTypeInfo(
                    characterType,
                    IsHot: false,
                    SlotLimit: null,
                    SlotName: characterType == CharacterType.Slot ? ctx.Request : null,
                    visibility));

                // Имя — значением поля-имени, если оно в проекте есть; иначе SaveFields имени не пишет.
                var nameField = ctx.ProjectInfo.CharacterNameField;
                var fields = nameField is null
                    ? FieldLayerContainer.Empty(ctx.ProjectInfo)
                    : new FieldLayerContainer(ctx.ProjectInfo, new Dictionary<int, string?> { [nameField.Id.ProjectFieldId] = ctx.Request });

                _ = ctx.SaveFields(character, fields, [ctx.ProjectInfo.GroupTree.RootGroupId]);

                if (nameField is null)
                {
                    character.CharacterName = ctx.Request;
                }

                return character;
            });

        return new CharacterIdentification(projectId, character.CharacterId);
    }

    private Task ApplyCore(ClaimIdentification claimId, RestoredClaimTarget target, bool isCreation)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.ManageClaims,
            ProjectActiveRequirement.AllowBlocked,
            target,
            async ctx =>
            {
                EnsureProjectBlocked(ctx.ProjectInfo);
                var claimSnapshot = ctx.CharacterClaimInfo;

                // 1. Решения — по снимкам, без мутаций.
                var oldCharacter = ctx.Character;
                var (newCharacter, newCharacterInfo) = (ctx.Character, ctx.CharacterInfo);
                var moved = false;
                // Заявка, принятая на шаблон, живёт на своём персонаже из него: шаблон в цели для принятой
                // заявки — не перемещение, а то же место (иначе повтор создал бы ещё одного персонажа).
                // Непринятую же заявку с такого персонажа на шаблон переносить надо.
                var staysOnCharacterFromSlot = ctx.Request.Character == ctx.CharacterInfo.OriginalCharacterSlotId
                    && (ctx.Request.Status ?? claimSnapshot.Status) is ClaimStatus.Approved or ClaimStatus.CheckedIn;
                if (ctx.Request.Character is { } characterId
                    && characterId != ctx.CharacterInfo.Id
                    && !staysOnCharacterFromSlot)
                {
                    if (characterId.ProjectId != ctx.ProjectInfo.ProjectId)
                    {
                        throw new ArgumentException($"Персонаж {characterId} из другого проекта", nameof(target));
                    }
                    (newCharacter, newCharacterInfo) = await ctx.LoadOtherCharacter(characterId);
                    moved = true;
                }

                var status = ctx.Request.Status is { } s && s != claimSnapshot.Status ? s : (ClaimStatus?)null;
                var finalStatus = status ?? claimSnapshot.Status;

                UserIdentification? responsible = ctx.Request.ResponsibleMaster is { } masterId && masterId != claimSnapshot.ResponsibleMasterId
                    ? masterId
                    : null;
                var responsibleMaster = responsible is null ? null : EnsureMaster(ctx.ProjectInfo, responsible);

                var fee = ctx.Request.ManualFee is { } f && f != ctx.Claim.CurrentFee ? f : (int?)null;

                if (!isCreation && !moved && status is null && responsible is null && fee is null)
                {
                    ctx.NothingChanged();
                    return;
                }

                // Принятая заявка на шаблон, как при одобрении, получает своего персонажа из шаблона.
                var fromSlot = finalStatus is ClaimStatus.Approved or ClaimStatus.CheckedIn
                    && newCharacterInfo.CharacterTypeInfo.CharacterType == CharacterType.Slot;

                if (finalStatus is ClaimStatus.Approved or ClaimStatus.CheckedIn)
                {
                    EnsureCanBeApprovedOn(newCharacterInfo, claimSnapshot.ClaimId);
                }

                // Потерянная заявка подана после бэкапа, и раз она на NPC, то, скорее всего, после бэкапа
                // мастер сделал его персонажем игрока (#5907). Старые живые заявки на NPC (до 2021 года это
                // было нормой) сюда не попадают: они есть в бэкапе. Статус — только заданный в цели, как
                // в плане: без него у оболочки статус по умолчанию, а не восстановленный. Повтор после
                // обрыва (оболочка уже вставлена) идёт не созданием — тип тогда не меняется.
                var becomesPlayerCharacter = isCreation
                    && ctx.Request.Status is not (null or ClaimStatus.DeclinedByMaster or ClaimStatus.DeclinedByUser)
                    && newCharacterInfo.CharacterTypeInfo.CharacterType == CharacterType.NonPlayer;

                // 2. Мутации.
                var changes = new List<string>();

                if (moved)
                {
                    ctx.Claim.CharacterId = newCharacterInfo.Id.CharacterId;
                    ctx.Claim.Character = newCharacter;
                    changes.Add($"персонаж — {newCharacter.CharacterName}");
                }

                if (becomesPlayerCharacter)
                {
                    ctx.SetRestoredCharacterSettings(newCharacter, new CharacterTypeInfo(
                        CharacterType.Player,
                        IsHot: false,
                        SlotLimit: null,
                        SlotName: null,
                        newCharacterInfo.CharacterTypeInfo.CharacterVisibility));
                    // TODO[Localize]
                    changes.Add($"персонаж «{newCharacter.CharacterName}» стал персонажем игрока");
                }

                if (fromSlot)
                {
                    var slot = newCharacter;
                    // Исчерпанный остаток не мешает: одобрение было на самом деле, остаток остаётся нулём.
                    newCharacter = await ctx.CreateCharacterFromSlot(slot, ctx.Claim.Player, allowExhaustedLimit: true);
                    ctx.Claim.Character = newCharacter;
                    ctx.Claim.CharacterId = newCharacter.CharacterId;
                    ctx.MarkChanged(slot);
                    // TODO[Localize]
                    changes.Add($"персонаж создан из шаблона «{slot.CharacterName}»");
                }

                if (status is { } newStatus)
                {
                    ctx.RestoreStatus(newStatus, ctx.Request.StatusChangedAt?.UtcDateTime ?? ctx.Now);
                    changes.Add($"статус — {GetStatusName(newStatus)}");
                }

                // Новая заявка тоже: она может прийти на удалённого в бэкапе персонажа.
                if (isCreation || moved || fromSlot || status is not null)
                {
                    SyncCharacters(ctx, oldCharacter, newCharacter);
                }

                if (fromSlot)
                {
                    // Как ApproveCore: пересохранение пустым слоем после принятия (ApprovedClaim уже
                    // проставлен) даёт персонажу из шаблона имя, переносит в него поля персонажа из
                    // заявки и пересчитывает спецгруппы. Режим восстановления — без значений
                    // по умолчанию и проверки обязательности: данные какие были.
                    _ = ctx.SaveRestoredFields(FieldLayerContainer.Empty(ctx.ProjectInfo));
                }

                if (responsibleMaster is not null)
                {
                    ctx.Claim.ResponsibleMasterUserId = responsibleMaster.UserId.Value;
                    changes.Add($"ответственный — {responsibleMaster.Name.DisplayName}");
                }

                if (fee is { } newFee)
                {
                    ctx.Claim.CurrentFee = newFee;
                    changes.Add($"взнос — {newFee}");
                }

                // TODO[Localize]
                var header = isCreation
                    ? "Заявка восстановлена по уведомлениям после потери данных"
                    : "Восстановлено по уведомлениям после потери данных";
                _ = ctx.AddHistoryRecordWithoutNotification(
                    changes.Count == 0 ? header + "." : header + ": " + string.Join(", ", changes) + ".",
                    CommentExtraAction.RestoredFromNotifications);
            });

    /// <summary>Дата из уведомлений: не из будущего и не «пустая» (ниже — переполнение <c>datetime</c>).</summary>
    private static void EnsureRestoredDate(DateTimeOffset at, DateTime now)
    {
        OperationDateValidation.CheckOperationDate(at.UtcDateTime, now);
        if (at.Year < 2000)
        {
            throw new ArgumentOutOfRangeException(nameof(at), at, "Слишком ранняя дата для восстановленной заявки");
        }
    }

    private static ProjectMasterInfo EnsureMaster(ProjectInfo projectInfo, UserIdentification userId)
        => projectInfo.Masters.SingleOrDefault(m => m.UserId == userId)
            // TODO[Localize]
            ?? throw new JoinValidationException($"Ответственный {userId} не мастер проекта. Сначала восстановите ему доступ.");

    /// <summary>
    /// Принятой заявка может стать только на персонаже, у которого нет другой принятой заявки (инвариант
    /// снимка, ADR013). Шаблон не проверяется: принятая заявка на него, как при одобрении, переезжает на
    /// своего, только что созданного из шаблона персонажа, а у того других заявок нет.
    /// </summary>
    private static void EnsureCanBeApprovedOn(CharacterInfo character, ClaimIdentification claimId)
    {
        if (character.CharacterTypeInfo.CharacterType == CharacterType.Slot)
        {
            return;
        }
        if (character.Claims.FirstOrDefault(c => c.IsApproved && c.ClaimId != claimId) is { } other)
        {
            // TODO[Localize]
            throw new JoinValidationException(
                $"У персонажа «{character.CharacterName}» уже есть принятая заявка {other.ClaimId.ClaimId}. Сначала восстановите её.");
        }
    }

    /// <summary>
    /// После перемещения и смены статуса: ссылка персонажа на принятую заявку, «игрок на игре»,
    /// активность персонажа и отметка аудита у каждого затронутого персонажа.
    /// </summary>
    private static void SyncCharacters(ClaimMutationContext ctx, Character oldCharacter, Character newCharacter)
    {
        var claim = ctx.Claim;

        if (oldCharacter != newCharacter && oldCharacter.ApprovedClaimId == claim.ClaimId)
        {
            oldCharacter.ApprovedClaimId = null;
            oldCharacter.ApprovedClaim = null;
            oldCharacter.InGame = false;
        }
        ctx.MarkChanged(oldCharacter);

        if (claim.IsApproved)
        {
            newCharacter.ApprovedClaimId = claim.ClaimId;
            newCharacter.ApprovedClaim = claim;
            newCharacter.IsHot = false;
            newCharacter.InGame = claim.ClaimStatus == ClaimStatus.CheckedIn;
        }
        else if (newCharacter.ApprovedClaimId == claim.ClaimId)
        {
            newCharacter.ApprovedClaimId = null;
            newCharacter.ApprovedClaim = null;
            newCharacter.InGame = false;
        }

        // Как в RestoreByMaster: живая заявка на удалённого персонажа возвращает его.
        if (claim.IsPending)
        {
            newCharacter.IsActive = true;
        }
        ctx.MarkChanged(newCharacter);
    }

    private static void EnsureProjectBlocked(ProjectInfo projectInfo)
    {
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.Blocked)
        {
            // TODO[Localize]
            throw new JoinValidationException("Восстановление из уведомлений доступно только в заблокированном проекте.");
        }
    }

    // TODO[Localize] — названия как в ClaimStatusView.
    private static string GetStatusName(ClaimStatus status) => status switch
    {
        ClaimStatus.AddedByUser => "подана",
        ClaimStatus.AddedByMaster => "предложена",
        ClaimStatus.Approved => "принята",
        ClaimStatus.DeclinedByUser => "отозвана",
        ClaimStatus.DeclinedByMaster => "отклонена",
        ClaimStatus.Discussed => "обсуждается",
        ClaimStatus.OnHold => "в листе ожидания",
        ClaimStatus.CheckedIn => "игрок заехал",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
    };
}
