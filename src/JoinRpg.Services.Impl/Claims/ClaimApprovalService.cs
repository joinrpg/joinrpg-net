using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Утверждение заявки мастером. Вынесено из <c>ClaimServiceImpl</c> отдельным сервисом, чтобы
/// автоприём (<see cref="ClaimAutoApproveService"/>) мог зависеть от узкого контракта
/// <see cref="IClaimApprovalService"/>, а не от всего <c>IClaimService</c>: иначе получилась бы
/// циклическая зависимость в DI (ADR014, §7).
/// </summary>
internal class ClaimApprovalService(ICharacterPropsService characterPropsService) : IClaimApprovalService
{
    public Task ApproveByMaster(ClaimIdentification claimId, string commentText)
        => characterPropsService.ChangeClaimAsync(
            claimId,
            ClaimAccessRequirement.ApprovalDecline,
            ProjectActiveRequirement.MustBeActive,
            commentText ?? "",
            async ctx =>
            {
                if (ctx.Claim.ClaimStatus == ClaimStatus.CheckedIn)
                {
                    throw new ClaimWrongStatusException(ctx.Claim.GetId(), ctx.Claim.ClaimStatus);
                }

                if (ctx.Claim.Character.CharacterType == CharacterType.Slot)
                {
                    // Единственная асинхронная загрузка метода, и она условная: сюжеты нужны только
                    // слоту. Поднимать её наверх нельзя — грузились бы на каждое утверждение.
                    var character = await CreateCharacterFromSlot(ctx, ctx.Claim.Character, ctx.Claim.Player);
                    ctx.Claim.Character = character;
                    ctx.Claim.CharacterId = character.CharacterId;
                }

                ctx.ChangeStatus(ctx.Claim, ClaimStatus.Approved);

                _ = ctx.AddComment(
                    ctx.Request,
                    CommentExtraAction.ApproveByMaster,
                    ClaimOperationType.MasterVisibleChange);

                if (ctx.ProjectInfo.ClaimSettings.StrictlyOneCharacter)
                {
                    // Читает claim.Player.Claims — навигацию, которую write-хэндл грузит явно
                    // (Include(c => c.Player.Claims)). Без неё список молча оказался бы пуст.
                    foreach (var otherClaim in ctx.Claim.OtherPendingClaimsForThisPlayer())
                    {
                        ctx.ChangeStatus(otherClaim, ClaimStatus.DeclinedByMaster);

                        _ = ctx.AddComment(
                            otherClaim,
                            //TODO[Localize]
                            "Заявка автоматически отклонена, т.к. другая заявка того же игрока была принята в тот же проект",
                            CommentExtraAction.DeclineByMaster,
                            ClaimOperationType.MasterVisibleChange);
                    }
                }

                ctx.MarkCharacterChangedIfApproved();
                ctx.Claim.Character.ApprovedClaimId = ctx.Claim.ClaimId;
                // Порядок критичен: ApprovedClaim обязан быть проставлен ДО SaveFields — от него
                // зависит выбор стратегии в FieldSaveHelper (IsApproved => SaveToCharacterAndClaim).
                ctx.Claim.Character.ApprovedClaim = ctx.Claim;
                ctx.Claim.Character.IsHot = false;

                // Пересохранение пустым слоем — не мёртвый код, оно нужно ради побочных эффектов:
                // 1. если персонаж создан из слота при утверждении, ему надо проставить имя;
                // 2. часть значений полей переезжает из заявки в персонажа;
                // 3. (2) может пересчитать спецгруппы.
                // Показывать изменённые поля в письме не надо, поэтому результат игнорируем.
                _ = ctx.SaveFields(FieldLayerContainer.Empty(ctx.ProjectInfo));
            },
            operationName: nameof(ApproveByMaster));

    /// <summary>
    /// Создаёт персонажа из слота при утверждении заявки: уменьшает остаток слота, копирует
    /// свойства и наследует прямые привязки к сюжетам.
    /// </summary>
    /// <remarks>
    /// <c>CreatedAt</c>/<c>UpdatedAt</c> проставляются локальным <see cref="DateTime.Now"/>, а не
    /// временем операции (UTC). Это выглядит ошибкой, но сохранено как есть: миграция обязана
    /// сохранять поведение, а починка — отдельное изменение.
    /// </remarks>
    private static async Task<Character> CreateCharacterFromSlot(ClaimMutationContext ctx, Character slot, User player)
    {
        switch (slot.CharacterSlotLimit)
        {
            case null:  // Unlimited slot
                break;
            case > 0:
                slot.CharacterSlotLimit--;
                break;
            default:
                throw new JoinRpgSlotLimitedException(slot);
        }

        if (slot.CharacterType != CharacterType.Slot)
        {
            throw new EntityWrongStatusException(slot);
        }

        var newCharacter = new Character()
        {
            ApprovedClaim = null,
            ApprovedClaimId = null,
            AutoCreated = true,
            OriginalCharacterSlot = slot,
            CanBePermanentlyDeleted = false,
            CharacterId = -1,
            CharacterName = slot.CharacterName, // Probably will be updated by field save
            CharacterSlotLimit = null,
            CharacterType = CharacterType.Player,
            CreatedAt = DateTime.Now,
            CreatedBy = player,
            CreatedById = player.UserId,
            DirectlyRelatedPlotElements = slot.DirectlyRelatedPlotElements,
            HidePlayerForCharacter = slot.HidePlayerForCharacter,
            InGame = false,
            IsAcceptingClaims = true,
            IsActive = true,
            IsHot = false,
            IsPublic = slot.IsPublic,
            JsonData = slot.JsonData,
            ParentCharacterGroupIds = slot.ParentCharacterGroupIds,
            PlotElementOrderData = slot.PlotElementOrderData,
            Project = slot.Project,
            ProjectId = slot.ProjectId,
            Subscriptions = slot.Subscriptions,
            UpdatedAt = DateTime.Now,
            UpdatedBy = player,
            UpdatedById = player.UserId,
        };

        // Через контекст, а не через UnitOfWork.GetDbSet: сохраняет тот DbContext, который загрузил
        // агрегат, а DI-экземпляр UnitOfWork у сервиса — другой (ADR014).
        ctx.AddEntity(newCharacter);

        var plots = await ctx.LoadDirectPlotsForCharacter(slot.GetId());

        foreach (var plot in plots)
        {
            if (plot.TargetCharacters.Contains(slot))
            {
                plot.TargetCharacters.Add(newCharacter);
            }
        }

        return newCharacter;
    }
}
