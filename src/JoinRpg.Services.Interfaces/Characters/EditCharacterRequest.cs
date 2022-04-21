using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Characters;

/// <summary>
/// Request from master to edit character
/// </summary>
public record EditCharacterRequest(
    CharacterIdentification Id,
    bool IsPublic,
    IReadOnlyCollection<int> ParentCharacterGroupIds,
    CharacterTypeInfo CharacterTypeInfo,
    bool HidePlayerForCharacter,
    IReadOnlyDictionary<int, string?> FieldValues,
    string? Name = null)
{ }
