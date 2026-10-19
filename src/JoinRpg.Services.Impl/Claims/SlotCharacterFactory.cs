using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Создание персонажа из шаблона (слота) при принятии заявки на него. Общее для одобрения мастером
/// и восстановления принятой заявки по уведомлениям (DRP, #5645).
/// </summary>
internal static class SlotCharacterFactory
{
    /// <summary>
    /// Создаёт персонажа из шаблона: уменьшает остаток шаблона, копирует свойства и наследует прямые
    /// привязки к сюжетам. Заявку на нового персонажа не переносит — это дело вызывающего.
    /// </summary>
    /// <param name="allowExhaustedLimit">
    /// <c>false</c> — исчерпанный шаблон не принимает заявку (<see cref="JoinRpgSlotLimitedException"/>),
    /// так при одобрении. <c>true</c> — для восстановления: одобрение уже случилось на самом деле, и
    /// исчерпанный остаток просто остаётся нулём.
    /// </param>
    /// <remarks>
    /// <c>CreatedAt</c>/<c>UpdatedAt</c> проставляются локальным <see cref="DateTime.Now"/>, а не
    /// временем операции (UTC). Это выглядит ошибкой, но сохранено как есть: миграция обязана
    /// сохранять поведение, а починка — отдельное изменение.
    /// </remarks>
    public static async Task<Character> CreateCharacterFromSlot(
        this ClaimMutationContext ctx,
        Character slot,
        User player,
        bool allowExhaustedLimit)
    {
        switch (slot.CharacterSlotLimit)
        {
            case null:  // Unlimited slot
                break;
            case > 0:
                slot.CharacterSlotLimit--;
                break;
            default:
                if (!allowExhaustedLimit)
                {
                    throw new JoinRpgSlotLimitedException(slot);
                }
                break;
        }

        if (slot.CharacterType != CharacterType.Slot)
        {
            throw new EntityWrongStatusException(slot);
        }

        // Сюжеты грузятся до создания персонажа: прямые привязки берутся из этой выборки, а не из
        // slot.DirectlyRelatedPlotElements — навигация на write-хэндле не загружена, и обращение к
        // ней стоило бы ленивой загрузки (#5061).
        var plots = await ctx.LoadDirectPlotsForCharacter(slot.GetId());

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
            DirectlyRelatedPlotElements = [.. plots],
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
