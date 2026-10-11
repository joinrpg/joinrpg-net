using JoinRpg.DataModel;
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
internal class ClaimRestoreService(ICharacterPropsService characterPropsService) : IClaimRestoreService
{
    public Task ApplyRestoredState(ClaimIdentification claimId, RestoredClaimTarget target)
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
                var responsibleMaster = responsible is null ? null : ctx.ProjectInfo.Masters.SingleOrDefault(m => m.UserId == responsible)
                    // TODO[Localize]
                    ?? throw new JoinValidationException($"Ответственный {responsible} не мастер проекта. Сначала восстановите ему доступ.");

                var fee = ctx.Request.ManualFee is { } f && f != ctx.Claim.CurrentFee ? f : (int?)null;

                if (!moved && status is null && responsible is null && fee is null)
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

                if (moved || status is not null)
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
                _ = ctx.AddHistoryRecordWithoutNotification(
                    "Восстановлено по уведомлениям после потери данных: " + string.Join(", ", changes) + ".",
                    CommentExtraAction.RestoredFromNotifications);
            });

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
