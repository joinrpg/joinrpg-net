using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain;

public static class CharacterTypeSettingsExtensions
{
    /// <summary>
    /// Раскладывает настройки типа по полям сущности персонажа. Инвариант «тип нельзя менять, пока
    /// есть активные заявки» здесь не проверяется — он живёт в
    /// <see cref="CharacterInfo.EnsureCanChangeTypeTo"/>, и проверять его или нет, решает операция.
    /// </summary>
    /// <exception cref="FieldRequiredException">Слоту без поля-имени не задано имя.</exception>
    public static void ApplyTypeSettings(this Character character, CharacterTypeInfo characterTypeInfo, ProjectInfo projectInfo)
    {
        (character.CharacterType,
            character.IsHot,
            character.CharacterSlotLimit,
            character.IsAcceptingClaims,
            _,
            character.IsPublic,
            character.HidePlayerForCharacter) = characterTypeInfo;

        if (characterTypeInfo.GetSlotCharacterName(projectInfo) is { } slotName)
        {
            character.CharacterName = slotName;
        }

        character.IsActive = true;
    }
}
