using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    void AddCharacterGroup(int projectId, string name, bool isPublic, List<int> parentCharacterGroupIds);
    void AddCharacter(int projectId, List<int> parentCharacterGroupIds, string name, bool isPublic, bool isAcceptingClaims);
  }
}
