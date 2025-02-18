using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Data.Interfaces;

public interface IProjectRepository : IDisposable
{
    Task<IReadOnlyCollection<ProjectWithClaimCount>> GetActiveProjectsWithClaimCount(
        int? userId);

    Task<IReadOnlyCollection<ProjectWithClaimCount>> GetArchivedProjectsWithClaimCount(
        int? userId);

    Task<IReadOnlyCollection<ProjectWithClaimCount>> GetAllProjectsWithClaimCount(int? userId);

    Task<IEnumerable<Project>> GetMyActiveProjectsAsync(int userInfoId);

    Task<IEnumerable<Project>> GetActiveProjectsWithSchedule();
    Task<Project> GetProjectAsync(int project);
    Task<Project> GetProjectWithDetailsAsync(int project);
    Task<Project?> GetProjectWithFieldsAsync(int project);

    [Obsolete]
    Task<CharacterGroup?> GetGroupAsync(int projectId, int characterGroupId);

    Task<CharacterGroup?> GetGroupAsync(CharacterGroupIdentification characterGroupId);

    Task<CharacterGroup> GetRootGroupAsync(int projectId);

    Task<CharacterGroup?> LoadGroupWithTreeAsync(int projectId, int? characterGroupId = null);
    Task<CharacterGroup> LoadGroupWithTreeSlimAsync(int projectId);
    Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId);
    Task<IList<CharacterGroup>> LoadGroups(int projectId, IReadOnlyCollection<int> groupIds);

    Task<IList<CharacterGroup>> LoadGroups(IReadOnlyCollection<CharacterGroupIdentification> groupIds);

    Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(int projectId,
         IReadOnlyCollection<int> characterIds);

    Task<ProjectField> GetProjectField(int projectId, int projectCharacterFieldId);

    Task<ProjectField> GetProjectField(ProjectFieldIdentification projectFieldId);

    Task<ProjectFieldDropdownValue> GetFieldValue(int projectId,
        int projectFieldId,
        int projectCharacterFieldDropdownValueId);

    Task<ProjectFieldDropdownValue> GetFieldValue(ProjectFieldIdentification projectFieldId,
    int projectCharacterFieldDropdownValueId);

    Task<Project> GetProjectWithFinances(int projectid);
    Task<Project> GetProjectForFinanceSetup(int projectid);
    Task<ICollection<Character>> GetCharacters(int projectId);
    Task<ICollection<Character>> GetCharacterByGroups(int projectId, int[] characterGroupIds);

    /// <summary>
    /// Get projects not active since
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetStaleProjects(DateTime inActiveSince);

    Task<ProjectHeaderDto[]> GetMyProjects(UserIdentification userIdentification);
}

public record ProjectHeaderDto(ProjectIdentification ProjectId, string ProjectName, bool IAmMaster, bool HasActiveClaims) : ILinkableWithName
{
    string ILinkableWithName.Name => ProjectName;

    LinkType ILinkable.LinkType => LinkType.Project;

    string? ILinkable.Identification => null;

    int? ILinkable.ProjectId => ProjectId.Value;
}
