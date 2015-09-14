using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Services.Interfaces
{
  public interface IProjectService
  {
    Project AddProject(string projectName, User creator);
    void AddCharacterField(ProjectCharacterField field);
    void UpdateCharacterField(int projectId, int fieldId, string name, string fieldHint, bool canPlayerEdit, bool canPlayerView, bool isPublic);
    void DeleteField(int projectCharacterFieldId);
    void AddCharacterGroup(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds, string description);
    void AddCharacter(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds, bool isAcceptingClaims, string description);

    Task EditCharacterGroup(int projectId, int characterGroupId, string name, bool isPublic,
      List<int> parentCharacterGroupIds, string description, bool haveDirectSlots, int directSlots);
    void DeleteCharacterGroup(int projectId, int characterGroupId);
    Task EditProject(int projectId, string projectName, string claimApplyRules, string projectAnnounce);
    void GrantAccess(int projectId, int userId, bool canGrantRights, bool canChangeFields, bool canChangeProjectProperties);
    void SaveCharacterFields(int projectId, int characterId, int currentUserId, string characterName, IDictionary<int, string> newFieldValue);
  }
}
