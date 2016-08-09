using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IProjectRepository : IDisposable
  {
    Task<IEnumerable<Project>> GetActiveProjectsWithClaimCount();
    Task<IEnumerable<Project>> GetArchivedProjectsWithClaimCount();
    Task<IEnumerable<Project>> GetAllProjectsWithClaimCount();
    IEnumerable<Project> GetMyActiveProjects(int? userInfoId);
    Task<Project> GetProjectAsync(int project);
    Task<Project> GetProjectWithDetailsAsync(int project);
    Task<CharacterGroup> GetGroupAsync(int projectId, int characterGroupId);
    Task<CharacterGroup> LoadGroupWithTreeAsync(int projectId, int characterGroupId);
    Task<CharacterGroup> LoadGroupWithTreeAsync(int projectId);
    Task<CharacterGroup> LoadGroupWithTreeSlimAsync(int projectId);
    Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId);
    Task<Character> GetCharacterAsync(int projectId, int characterId);
    Task<Character> GetCharacterWithGroups(int projectId, int characterId);
    Task<Character> GetCharacterWithDetails(int projectId, int characterId);
    Task<IList<CharacterGroup>> LoadGroups(int projectId, IReadOnlyCollection<int> groupIds);
    Task<IReadOnlyCollection<Character>> LoadCharacters(int projectId, [NotNull] IReadOnlyCollection<int> characterIds);
    Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(int projectId, [NotNull] IReadOnlyCollection<int> characterIds);
    Task<ProjectField> GetProjectField(int projectId, int projectCharacterFieldId);
    Task<ProjectFieldDropdownValue> GetFieldValue(int projectId, int projectFieldId, int projectCharacterFieldDropdownValueId);
    Task<Project> GetProjectWithFinances(int projectid);
    Task<Project> GetProjectForFinanceSetup(int projectid);
    Task<ICollection<Character>> GetCharacters(int projectId);
    Task<IEnumerable<Project>>  GetProjectsWithoutAllrpgAsync();
  }
}
