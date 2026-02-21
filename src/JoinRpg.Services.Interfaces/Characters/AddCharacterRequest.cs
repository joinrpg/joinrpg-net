namespace JoinRpg.Services.Interfaces.Characters;

/// <summary>
/// Request from master to create new character
/// </summary>
public record AddCharacterRequest(
    ProjectIdentification ProjectId,
    IReadOnlyCollection<CharacterGroupIdentification> ParentCharacterGroupIds,
    CharacterTypeInfo CharacterTypeInfo,
    IReadOnlyDictionary<int, string?> FieldValues)
{ }
