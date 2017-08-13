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
    [Obsolete("Use GetMyActiveProjectsAsync")]
    IEnumerable<Project> GetMyActiveProjects(int? userInfoId);
    Task<IEnumerable<Project>> GetMyActiveProjectsAsync(int userInfoId);
    Task<Project> GetProjectAsync(int project);
    Task<Project> GetProjectWithDetailsAsync(int project);
    Task<Project> GetProjectWithFieldsAsync(int project);
    [NotNull, ItemCanBeNull]
    Task<CharacterGroup> GetGroupAsync(int projectId, int characterGroupId);
    Task<CharacterGroup> LoadGroupWithTreeAsync(int projectId, int? characterGroupId = null);
    Task<CharacterGroup> LoadGroupWithTreeSlimAsync(int projectId);
    Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId);
    Task<IList<CharacterGroup>> LoadGroups(int projectId, IReadOnlyCollection<int> groupIds);
    Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(int projectId, [NotNull] IReadOnlyCollection<int> characterIds);
    Task<ProjectField> GetProjectField(int projectId, int projectCharacterFieldId);
    Task<ProjectFieldDropdownValue> GetFieldValue(int projectId, int projectFieldId, int projectCharacterFieldDropdownValueId);
    Task<Project> GetProjectWithFinances(int projectid);
    Task<Project> GetProjectForFinanceSetup(int projectid);
    Task<ICollection<Character>> GetCharacters(int projectId);
    Task<ICollection<Character>> GetCharacterByGroups(int projectId, int[] characterGroupIds);
    Task<IClaimSource> GetClaimSource(int projectId, int? characterGroupId, int? characterId);
    [NotNull, ItemNotNull]
    Task<IReadOnlyCollection<CharacterGroup>> GetGroupsWithResponsible(int projectId);
  }
}
