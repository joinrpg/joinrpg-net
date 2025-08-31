using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

public interface IProjectRepository : IDisposable
{
    Task<IReadOnlyCollection<ProjectWithClaimCount>> GetActiveProjectsWithClaimCount(
        int? userId);

    Task<IReadOnlyCollection<ProjectWithClaimCount>> GetArchivedProjectsWithClaimCount(
        int? userId);

    Task<IReadOnlyCollection<ProjectWithClaimCount>> GetAllProjectsWithClaimCount(int? userId);

    Task<Project> GetProjectAsync(int project);
    [Obsolete]
    Task<Project> GetProjectWithDetailsAsync(int project);
    Task<Project?> GetProjectWithFieldsAsync(int project);

    [Obsolete]
    Task<CharacterGroup?> GetGroupAsync(int projectId, int characterGroupId);

    Task<CharacterGroup?> GetGroupAsync(CharacterGroupIdentification characterGroupId);

    Task<CharacterGroup?> LoadGroupWithTreeAsync(int projectId, int? characterGroupId = null);
    Task<CharacterGroup> LoadGroupWithTreeSlimAsync(int projectId);
    Task<CharacterGroup> LoadGroupWithChildsAsync(int projectId, int characterGroupId);
    Task<IList<CharacterGroup>> LoadGroups(int projectId, IReadOnlyCollection<int> groupIds);

    Task<IList<CharacterGroup>> LoadGroups(IReadOnlyCollection<CharacterGroupIdentification> groupIds);

    Task<ProjectField> GetProjectField(int projectId, int projectCharacterFieldId);

    Task<ProjectField> GetProjectField(ProjectFieldIdentification projectFieldId);

    Task<ProjectFieldDropdownValue> GetFieldValue(int projectId,
        int projectFieldId,
        int projectCharacterFieldDropdownValueId);

    Task<ProjectFieldDropdownValue> GetFieldValue(ProjectFieldIdentification projectFieldId,
    int projectCharacterFieldDropdownValueId);

    Task<Project> GetProjectWithFinances(int projectid);
    Task<Project> GetProjectForFinanceSetup(int projectid);

    Task<ICollection<Character>> GetCharacterByGroups(int projectId, int[] characterGroupIds);

    /// <summary>
    /// Get projects not active since
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetStaleProjects(DateTime inActiveSince);

    Task<ProjectHeaderDto[]> GetProjectsBySpecification(UserIdentification userIdentification, ProjectListSpecification projectListSpecification);

    Task<CharacterGroupHeaderDto[]> LoadDirectChildGroupHeaders(CharacterGroupIdentification characterGroupId);
}

public record ProjectListSpecification(ProjectListCriteria Criteria, bool LoadArchived)
{
    public static ProjectListSpecification MyActiveProjects { get; } = new ProjectListSpecification(ProjectListCriteria.MasterOrActiveClaim, LoadArchived: false);
    public static ProjectListSpecification ForCloning { get; } = new ProjectListSpecification(ProjectListCriteria.ForCloning, LoadArchived: true);
    public static ProjectListSpecification ActiveWithMyMasterAccess { get; } = new ProjectListSpecification(ProjectListCriteria.MasterAccess, LoadArchived: false);
    public static ProjectListSpecification ActiveProjectsWithSchedule { get; } = new ProjectListSpecification(ProjectListCriteria.HasSchedule, LoadArchived: false);

    public static ProjectListSpecification ActiveProjectsWithoutKogdaIgra { get; } = new ProjectListSpecification(ProjectListCriteria.NoKogdaIgra, LoadArchived: false);
}

public enum ProjectListCriteria { MasterAccess, MasterOrActiveClaim, ForCloning, HasSchedule, NoKogdaIgra };

public record ProjectHeaderDto(ProjectIdentification ProjectId, string ProjectName, bool IAmMaster, bool HasActiveClaims) : ILinkableWithName
{
    string ILinkableWithName.Name => ProjectName;

    LinkType ILinkable.LinkType => LinkType.Project;

    string? ILinkable.Identification => null;

    int? ILinkable.ProjectId => ProjectId.Value;
}

public record CharacterGroupHeaderDto(CharacterGroupIdentification CharacterGroupId, string Name, bool IsActive, bool IsPublic) : ILinkableWithName
{
    string ILinkableWithName.Name => Name;

    LinkType ILinkable.LinkType => LinkType.ResultCharacterGroup;

    string? ILinkable.Identification => CharacterGroupId.CharacterGroupId.ToString();

    int? ILinkable.ProjectId => CharacterGroupId.ProjectId;
}
