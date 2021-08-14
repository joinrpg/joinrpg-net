using System.Collections.Generic;
using System.Threading.Tasks;

namespace JoinRpg.Services.Interfaces.Characters
{
    public interface ICharacterService
    {
        Task AddCharacter(AddCharacterRequest addCharacterRequest);

        Task DeleteCharacter(int projectId, int characterId, int currentUserId);

        Task EditCharacter(int currentUserId,
            int characterId,
            int projectId,
            string name,
            bool isPublic,
            IReadOnlyCollection<int> parentCharacterGroupIds,
            bool isAcceptingClaims,
            bool hidePlayerForCharacter,
            IReadOnlyDictionary<int, string?> characterFields,
            bool isHot);

        Task MoveCharacter(int currentUserId,
            int projectId,
            int characterId,
            int parentCharacterGroupId,
            short direction);

        Task SetFields(int projectId, int characterId, Dictionary<int, string?> requestFieldValues);
    }
}
