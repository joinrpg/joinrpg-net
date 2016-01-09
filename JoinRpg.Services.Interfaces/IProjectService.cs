using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IProjectService 
  {
    Task<Project> AddProject(string projectName, User creator);

    Task AddCharacterGroup(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds,
      string description, bool haveDirectSlotsForSave, int directSlotsForSave, int? responsibleMasterId);

    Task AddCharacter(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds, bool isAcceptingClaims, string description, bool hidePlayerForCharacter, bool isHot);

    Task EditCharacterGroup(int projectId, int characterGroupId, string name, bool isPublic,
      List<int> parentCharacterGroupIds, string description, bool haveDirectSlots, int directSlots,
      int? responsibleMasterId);

    Task DeleteCharacterGroup(int projectId, int characterGroupId);
    Task EditProject(int projectId, string projectName, string claimApplyRules, string projectAnnounce, bool isAcceptingClaims);

    Task GrantAccess(int projectId, int currentUserId, int userId, bool canGrantRights, bool canChangeFields, bool canChangeProjectProperties, bool canApproveClaims, bool canEditRoles, bool canAcceptCash, bool canManageMoney);

    Task SaveCharacterFields(int projectId, int characterId, int currentUserId, IDictionary<int, string> newFieldValue);

    Task RemoveAccess(int projectId, int currentUserId, int userId);

    Task ChangeAccess(int projectId, int currentUserId, int userId, bool canGrantRights, bool canChangeFields, bool canChangeProjectProperties, bool canApproveClaims, bool canEditRoles, bool canAcceptCash, bool canManageMoney);

    Task UpdateSubscribeForGroup(int projectId, int characterGroupId, int currentUserId, bool claimStatusChangeValue, bool commentsValue, bool fieldChangeValue, bool moneyOperationValue);

    Task DeleteCharacter(int projectId, int characterId);

    Task EditCharacter(int currentUserId, int characterId, int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds, bool isAcceptingClaims, string contents, bool hidePlayerForCharacter, IDictionary<int, string> characterFields, bool isHot);

    Task MoveCharacterGroup(int currentUserId, int projectId, int charactergroupId, int parentCharacterGroupId, short direction);

    Task MoveCharacter(int currentUserId, int projectId, int characterId, int parentCharacterGroupId, short direction);
  }
}
