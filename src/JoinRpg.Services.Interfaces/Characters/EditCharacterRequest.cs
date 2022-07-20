using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Characters;

/// <summary>
/// Request from master to edit character
/// </summary>
public record EditCharacterRequest(
    CharacterIdentification Id,
    IReadOnlyCollection<int> ParentCharacterGroupIds,
    CharacterTypeInfo CharacterTypeInfo,
    IReadOnlyDictionary<int, string?> FieldValues)
{ }
