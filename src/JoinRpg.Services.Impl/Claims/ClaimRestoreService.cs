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
        _ = await unitOfWork.GetUsersRepository().GetRequiredUserInfo(request.Player);

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
            ?? await CreateCharacter(projectId, request.NewCharacterName!);

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
                var comments = ctx.Request.Comments;
                var playerAt = Later(
                    claim.LastPlayerCommentAt,
                    comments.Where(c => c.IsCommentByPlayer).Select(c => (DateTimeOffset?)c.CreatedAt.ToUniversalTime()).Max());
                var (masterAt, masterBy) = LatestMaster(
                    claim.LastMasterCommentAt, claim.LastMasterCommentBy_Id, comments.Where(c => !c.IsCommentByPlayer));
                var (visibleAt, visibleBy) = LatestMaster(
                    claim.LastVisibleMasterCommentAt, claim.LastVisibleMasterCommentBy_Id, comments.Where(c => !c.IsCommentByPlayer && c.IsVisibleToPlayer));

                if (ctx.Request.Inserted == 0
                    && playerAt == claim.LastPlayerCommentAt
                    && masterAt == claim.LastMasterCommentAt && masterBy == claim.LastMasterCommentBy_Id
                    && visibleAt == claim.LastVisibleMasterCommentAt && visibleBy == claim.LastVisibleMasterCommentBy_Id)
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
                claim.LastPlayerCommentAt = playerAt;
                claim.LastMasterCommentAt = masterAt;
                claim.LastMasterCommentBy_Id = masterBy;
                claim.LastVisibleMasterCommentAt = visibleAt;
                claim.LastVisibleMasterCommentBy_Id = visibleBy;
                claim.LastUpdateDateTime = ctx.Now;
            });

    private static DateTimeOffset? Later(DateTimeOffset? before, DateTimeOffset? restored)
        => restored is { } r && (before is null || r > before) ? r : before;

    /// <summary>Отметка «последний мастерский комментарий»: прежняя либо самый поздний восстановленный, если он позже.</summary>
    private static (DateTimeOffset? At, int? By) LatestMaster(DateTimeOffset? beforeAt, int? beforeBy, IEnumerable<RestoredClaimComment> comments)
    {
        var latest = comments.MaxBy(c => c.CreatedAt);
        return latest is not null && (beforeAt is null || latest.CreatedAt > beforeAt)
            ? (latest.CreatedAt.ToUniversalTime(), latest.Author.Value)
            : (beforeAt, beforeBy);
    }

    /// <summary>
    /// Персонаж игрока в корневой группе: групп в уведомлениях нет, разложит мастер. Скрытый — какой
    /// была видимость, неизвестно, а скрытый ничего не раскроет до того, как мастер разберётся.
    /// </summary>
    private async Task<CharacterIdentification> CreateCharacter(ProjectIdentification projectId, string? name)
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
                    CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Private));

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
                if (ctx.Request.Character is { } characterId && characterId != ctx.CharacterInfo.Id)
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

                if (finalStatus is ClaimStatus.Approved or ClaimStatus.CheckedIn)
                {
                    EnsureCanBeApprovedOn(newCharacterInfo, claimSnapshot.ClaimId);
                }

                // 2. Мутации.
                var changes = new List<string>();

                if (moved)
                {
                    ctx.Claim.CharacterId = newCharacterInfo.Id.CharacterId;
                    ctx.Claim.Character = newCharacter;
                    changes.Add($"персонаж — {newCharacter.CharacterName}");
                }

                if (status is { } newStatus)
                {
                    ctx.RestoreStatus(newStatus, ctx.Request.StatusChangedAt?.UtcDateTime ?? ctx.Now);
                    changes.Add($"статус — {GetStatusName(newStatus)}");
                }

                // Новая заявка тоже: она может прийти на удалённого в бэкапе персонажа.
                if (isCreation || moved || status is not null)
                {
                    SyncCharacters(ctx, oldCharacter, newCharacter);
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
    /// снимка, ADR013), и не на слоте: одобрение слота создаёт из него нового персонажа, а этого
    /// восстановление не повторяет.
    /// </summary>
    private static void EnsureCanBeApprovedOn(CharacterInfo character, ClaimIdentification claimId)
    {
        if (character.CharacterTypeInfo.CharacterType == CharacterType.Slot)
        {
            // TODO[Localize]
            throw new JoinValidationException(
                $"Персонаж «{character.CharacterName}» — слот, принятую заявку на него не восстановить. Выберите персонажа.");
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
