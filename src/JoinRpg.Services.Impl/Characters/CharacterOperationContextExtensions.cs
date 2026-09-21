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
    public static void MarkCreatedNow(this CharacterOperationContext ctx, ICreatedUpdatedTrackedForEntity entity)
        => EntityAudit.MarkCreated(entity, ctx.Now, ctx.CurrentUser.UserId);

    /// <inheritdoc cref="MarkCreatedNow"/>
    public static void MarkChanged(this CharacterOperationContext ctx, ICreatedUpdatedTrackedForEntity entity)
        => EntityAudit.MarkChanged(entity, ctx.Now, ctx.CurrentUser.UserId);

    /// <summary>
    /// Список групп для персонажа: проверенный, либо — если проект запрещает мастерам выбирать
    /// группы — корневая группа. Перенос <c>CharacterServiceImpl.ValidateGroupListForCharacter</c>.
    /// </summary>
    public static int[] ValidateGroupListForCharacter(
        this CharacterOperationContext ctx,
        IReadOnlyCollection<CharacterGroupIdentification> groupIds)
        => ctx.ProjectInfo.AllowToSetGroups
            ? ctx.ProjectInfo.ValidateCharacterGroupList(ServiceValidation.Required(groupIds), ensureNotSpecial: true)
            : [ctx.ProjectInfo.RootCharacterGroupId.CharacterGroupId];

    /// <summary>
    /// Применяет к персонажу настройки типа. Инвариант: тип нельзя менять, пока есть активные
    /// заявки. Проверяется по доменному снимку, а не по EF-графу.
    /// </summary>
    public static void SetCharacterSettings(
        this CharacterMutationContext ctx,
        CharacterTypeInfo characterTypeInfo)
    {
        if (ctx.CharacterInfo.HasActiveClaims
            && characterTypeInfo.CharacterType != ctx.CharacterInfo.CharacterTypeInfo.CharacterType)
        {
            // TODO: заменить на типизированное исключение. Сейчас сохраняем ровно то, что бросал
            // CharacterServiceImpl до миграции, — см. раздел «что сознательно не чиним» в ADR014.
            throw new Exception("Can't change type of character with active claims");
        }

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
