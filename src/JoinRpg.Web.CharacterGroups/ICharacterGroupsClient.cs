using System.Collections.Generic;
using System.Threading.Tasks;

namespace JoinRpg.Web.CharacterGroups
{
    public interface ICharacterGroupsClient
    {
        Task<List<CharacterGroupDto>> GetCharacterGroups(int projectId);
    }
}
