using System.Collections.Generic;
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
    void EditCharacterGroup(int projectId, int characterGroupId, string name, bool isPublic, List<int> parentCharacterGroupIds, string description);
    void DeleteCharacterGroup(int projectId, int characterGroupId);
    void EditProject(int projectId, string projectName, string claimApplyRules);
    void GrantAccess(int projectId, int userId, bool canGrantRights, bool canChangeFields, bool canChangeProjectProperties);
    void SaveCharacterFields(int projectId, int characterId, int currentUserId, IDictionary<int,string> newFieldValue);
  }
}
