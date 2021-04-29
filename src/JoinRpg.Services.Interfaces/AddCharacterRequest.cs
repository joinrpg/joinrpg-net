using System.Collections.Generic;

namespace JoinRpg.Services.Interfaces
{
    public class AddCharacterRequest
    {
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public bool IsPublic { get; set; }
        public IReadOnlyCollection<int> ParentCharacterGroupIds { get; set; }
        public bool IsAcceptingClaims { get; set; }
        public bool HidePlayerForCharacter { get; set; }
        public bool IsHot { get; set; }
        public IReadOnlyDictionary<int, string?> FieldValues { get; set; }
    }
}
