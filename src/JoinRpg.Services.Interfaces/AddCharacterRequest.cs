using System.Collections.Generic;

namespace JoinRpg.Services.Interfaces
{
    public record AddCharacterRequest(
        int ProjectId,
        string Name,
        bool IsPublic,
        IReadOnlyCollection<int> ParentCharacterGroupIds,
        bool IsAcceptingClaims,
        bool HidePlayerForCharacter,
        bool IsHot,
        IReadOnlyDictionary<int, string?> FieldValues)
    { }
}
