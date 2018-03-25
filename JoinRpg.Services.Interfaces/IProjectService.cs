using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
    public interface IProjectService
    {
        Task<Project> AddProject(string projectName);

        Task EditProject(EditProjectRequest request);

        Task AddCharacterGroup(int projectId,
            string name,
            bool isPublic,
            IReadOnlyCollection<int> parentCharacterGroupIds,
            string description,
            bool haveDirectSlotsForSave,
            int directSlotsForSave,
            int? responsibleMasterId);

        Task AddCharacter(AddCharacterRequest addCharacterRequest);

        Task EditCharacterGroup(int projectId,
            int currentUserId,
            int characterGroupId,
            string name,
            bool isPublic,
            IReadOnlyCollection<int> parentCharacterGroupIds,
            string description,
            bool haveDirectSlots,
            int directSlots,
            int? responsibleMasterId);

        Task DeleteCharacterGroup(int projectId, int characterGroupId);

        Task GrantAccess(GrantAccessRequest grantAccessRequest);

        Task RemoveAccess(int projectId, int userId, int? newResponsibleMasterId);

        Task ChangeAccess(ChangeAccessRequest changeAccessRequest);

        Task UpdateSubscribeForGroup(SubscribeForGroupRequest request);

        Task DeleteCharacter(int projectId, int characterId, int currentUserId);

        Task EditCharacter(int currentUserId,
            int characterId,
            int projectId,
            string name,
            bool isPublic,
            IReadOnlyCollection<int> parentCharacterGroupIds,
            bool isAcceptingClaims,
            string contents,
            bool hidePlayerForCharacter,
            IReadOnlyDictionary<int, string> characterFields,
            bool isHot);

        Task MoveCharacterGroup(int currentUserId,
            int projectId,
            int charactergroupId,
            int parentCharacterGroupId,
            short direction);

        Task MoveCharacter(int currentUserId,
            int projectId,
            int characterId,
            int parentCharacterGroupId,
            short direction);

        Task CloseProject(int projectId, int currentUserId, bool publishPlot);

        Task SetCheckInOptions(int projectId,
            bool checkInProgress,
            bool enableCheckInModule,
            bool modelAllowSecondRoles);

        Task GrantAccessAsAdmin(int projectId);
    }
}
