using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Services.Impl.Characters;

internal static class CharacterOperationContextExtensions
{
    /// <summary>
    /// Отметка аудита для <b>прочих</b> задетых сущностей — второго персонажа при переносе, группы
    /// и т.п. Самого персонажа операции помечает сервис: любая операция над ним по определению его
    /// меняет, и полагаться на то, что каждый вызывающий об этом вспомнит, не стоит.
    /// </summary>
    /// <remarks>
    /// Парного <c>MarkCreatedNow</c> здесь нет: контекст персонажа сущностей не создаёт — создание
    /// идёт через <c>ICharacterPropsService.CreateCharacter</c>, где отметку ставит сам сервис.
    /// Одноимённое расширение существует у контекста проекта (ADR009), там оно и используется.
    /// </remarks>
    public static void MarkChanged(this CharacterOperationContext ctx, ICreatedUpdatedTrackedForEntity entity)
        => EntityAudit.MarkChanged(entity, ctx.Now, ctx.CurrentUser.UserId);

    /// <summary>
    /// Применяет к персонажу настройки типа. Инвариант «тип нельзя менять, пока есть активные
    /// заявки» живёт в <see cref="CharacterInfo.EnsureCanChangeTypeTo"/>.
    /// </summary>
    public static void SetCharacterSettings(
        this CharacterMutationContext ctx,
        CharacterTypeInfo characterTypeInfo)
    {
        ctx.CharacterInfo.EnsureCanChangeTypeTo(characterTypeInfo);

        ApplySettings(ctx.Character, characterTypeInfo, ctx.ProjectInfo);
    }

    /// <summary>
    /// То же для только что созданного персонажа: заявок у него ещё нет, проверять нечего.
    /// </summary>
    public static void SetCharacterSettings(
        this CharacterCreationContext ctx,
        Character character,
        CharacterTypeInfo characterTypeInfo)
        => ApplySettings(character, characterTypeInfo, ctx.ProjectInfo);

    private static void ApplySettings(Character character, CharacterTypeInfo characterTypeInfo, ProjectInfo projectInfo)
    {
        (character.CharacterType,
            character.IsHot,
            character.CharacterSlotLimit,
            character.IsAcceptingClaims,
            _,
            character.IsPublic,
            character.HidePlayerForCharacter) = characterTypeInfo;

        if (characterTypeInfo.CharacterType == CharacterType.Slot && projectInfo.CharacterNameField is null)
        {
            character.CharacterName = ServiceValidation.Required(characterTypeInfo.SlotName);
        }

        character.IsActive = true;
    }
}
