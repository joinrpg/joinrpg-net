using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Characters;

/// <summary>
/// Request from master to create new character
/// </summary>
public record AddCharacterRequest(
    int ProjectId,
    string? SlotName,
    bool IsPublic,
    IReadOnlyCollection<int> ParentCharacterGroupIds,
    CharacterTypeInfo CharacterTypeInfo,
    bool HidePlayerForCharacter,
    IReadOnlyDictionary<int, string?> FieldValues)
{ }
