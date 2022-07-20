using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Characters;

/// <summary>
/// Request from master to create new character
/// </summary>
public record AddCharacterRequest(
    int ProjectId,
    IReadOnlyCollection<int> ParentCharacterGroupIds,
    CharacterTypeInfo CharacterTypeInfo,
    IReadOnlyDictionary<int, string?> FieldValues)
{ }
