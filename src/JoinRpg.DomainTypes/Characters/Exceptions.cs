using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters;

/// <summary>
/// Персонажа нельзя удалить, потому что на него есть активные заявки.
/// </summary>
public class CharacterHasActiveClaimsException(CharacterIdentification characterId)
    : JoinRpgProjectException(characterId.ProjectId, $"Character {characterId} can't be deleted, because it has active claims")
{
    public CharacterIdentification CharacterId { get; } = characterId;
}

/// <summary>
/// Персонажа нельзя удалить, потому что он выбран шаблоном для новых персонажей проекта.
/// </summary>
public class DefaultTemplateCharacterCannotBeDeletedException(CharacterIdentification characterId)
    : JoinRpgProjectException(characterId.ProjectId, $"Character {characterId} can't be deleted, because it is the default template character of the project")
{
    public CharacterIdentification CharacterId { get; } = characterId;
}

/// <summary>
/// В операцию передана спецгруппа там, где допустимы только обычные группы:
/// спецгруппы проставляются автоматически по значениям полей, вручную их указывать нельзя.
/// </summary>
public class SpecialCharacterGroupNotAllowedException(CharacterGroupIdentification groupId)
    : JoinRpgProjectException(groupId.ProjectId, $"Special group {groupId} can't be used here, special groups are maintained automatically")
{
    public CharacterGroupIdentification GroupId { get; } = groupId;
}
