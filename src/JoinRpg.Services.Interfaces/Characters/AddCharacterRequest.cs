using System.Collections.Generic;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Characters
{
    /// <summary>
    /// Request from master to create new character
    /// </summary>
    public record AddCharacterRequest(
        int ProjectId,
        string Name,
        bool IsPublic,
        IReadOnlyCollection<int> ParentCharacterGroupIds,
        CharacterTypeInfo CharacterTypeInfo,
        bool HidePlayerForCharacter,
        IReadOnlyDictionary<int, string?> FieldValues)
    { }
}
