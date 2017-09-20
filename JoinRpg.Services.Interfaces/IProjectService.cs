using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IProjectService
  {
    Task<Project> AddProject(string projectName, User creator);

    Task AddCharacterGroup(int projectId, string name, bool isPublic, IReadOnlyCollection<int> parentCharacterGroupIds, string description, bool haveDirectSlotsForSave, int directSlotsForSave, int? responsibleMasterId);

    Task AddCharacter(int projectId, int currentUserId, string name, bool isPublic, IReadOnlyCollection<int> parentCharacterGroupIds, bool isAcceptingClaims, string description, bool hidePlayerForCharacter, bool isHot);

    Task EditCharacterGroup(int projectId, int currentUserId, int characterGroupId, string name, bool isPublic, IReadOnlyCollection<int> parentCharacterGroupIds, string description, bool haveDirectSlots, int directSlots, int? responsibleMasterId);

    Task DeleteCharacterGroup(int projectId, int characterGroupId);

    Task EditProject(int projectId, int currentUserId, string projectName, string claimApplyRules, string projectAnnounce, bool isAcceptingClaims, bool multipleCharacters, bool publishPlot);

    Task GrantAccess(int projectId, int currentUserId, int userId, bool canGrantRights, bool canChangeFields,
      bool canChangeProjectProperties, bool canApproveClaims, bool canEditRoles, bool canManageMoney,
      bool canSendMassMails, bool canManagePlots);

    Task RemoveAccess(int projectId, int userId, int? newResponsibleMasterId);

    Task ChangeAccess(int projectId, int currentUserId, int userId, bool canGrantRights, bool canChangeFields,
      bool canChangeProjectProperties, bool canApproveClaims, bool canEditRoles, bool canManageMoney,
      bool canSendMassMails, bool canManagePlots);

    Task UpdateSubscribeForGroup(int projectId, int characterGroupId, int currentUserId, bool claimStatusChangeValue,
      bool commentsValue, bool fieldChangeValue, bool moneyOperationValue);

    Task DeleteCharacter(int projectId, int characterId, int currentUserId);

    Task EditCharacter(int currentUserId, int characterId, int projectId, string name, bool isPublic,
      IReadOnlyCollection<int> parentCharacterGroupIds, bool isAcceptingClaims, string contents,
      bool hidePlayerForCharacter, IDictionary<int, string> characterFields, bool isHot);

    Task MoveCharacterGroup(int currentUserId, int projectId, int charactergroupId, int parentCharacterGroupId,
      short direction);

    Task MoveCharacter(int currentUserId, int projectId, int characterId, int parentCharacterGroupId, short direction);
    Task CloseProject(int projectId, int currentUserId, bool publishPlot);
    Task SetCheckInOptions(int projectId, bool checkInProgress, bool enableCheckInModule, bool modelAllowSecondRoles);
      Task GrantAccessAsAdmin(int projectId);
  }
}
