using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Data.Interfaces;

public interface IProjectRepository : IDisposable
{
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

    Task<ICollection<Character>> GetCharacterByGroups(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds);

    /// <summary>
    /// Get projects not active since
    /// </summary>
    /// <returns></returns>
    Task<IReadOnlyCollection<ProjectWithUpdateDateDto>> GetStaleProjects(DateTime inActiveSince);

    /// <summary>
    /// Проекты грузятся всегда относительно какого-то пользователя.
    /// Даже в тех местах, где речь не идет про доступ — нужно всегда сортировать «мои» проекты вперед
    /// </summary>
    Task<ProjectShortInfo[]> GetProjectsBySpecification(UserIdentification? userId, ProjectListSpecification projectListSpecification);

    /// <summary>
    /// Проекты грузятся всегда относительно какого-то пользователя.
    /// Даже в тех местах, где речь не идет про доступ — нужно всегда сортировать «мои» проекты вперед
    /// </summary>
    Task<ProjectShortInfo[]> GetProjectsByIds(UserIdentification? userId, ProjectIdentification[] ids);

    Task<CharacterGroupHeaderDto[]> LoadDirectChildGroupHeaders(CharacterGroupIdentification characterGroupId);

    Task<CharacterGroupHeaderDto[]> GetGroupHeaders(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds);
}

public record ProjectListSpecification(ProjectListCriteria Criteria, bool LoadArchived)
{
    public static ProjectListSpecification MyActiveProjects { get; } = new ProjectListSpecification(ProjectListCriteria.MasterOrActiveClaim, LoadArchived: false);
    public static ProjectListSpecification ForCloning { get; } = new ProjectListSpecification(ProjectListCriteria.ForCloning, LoadArchived: true);
    public static ProjectListSpecification ActiveWithMyMasterAccess { get; } = new ProjectListSpecification(ProjectListCriteria.MasterAccess, LoadArchived: false);
    public static ProjectListSpecification ActiveProjectsWithSchedule { get; } = new ProjectListSpecification(ProjectListCriteria.HasSchedule, LoadArchived: false);

    public static ProjectListSpecification ActiveProjectsWithoutKogdaIgra { get; } = new ProjectListSpecification(ProjectListCriteria.KogdaIgraMissing, LoadArchived: false);

    public static ProjectListSpecification AllProjectsWithMasterAccess { get; } = new ProjectListSpecification(ProjectListCriteria.MasterAccess, LoadArchived: true);

    public static ProjectListSpecification ActiveProjectsWithGrantMasterAccess { get; } = new ProjectListSpecification(ProjectListCriteria.MasterGrantAccess, LoadArchived: false);

    public static ProjectListSpecification All { get; } = new ProjectListSpecification(ProjectListCriteria.All, LoadArchived: true);

    public static ProjectListSpecification Active { get; } = new ProjectListSpecification(ProjectListCriteria.All, LoadArchived: false);
}

public enum ProjectListCriteria { MasterAccess, MasterOrActiveClaim, ForCloning, HasSchedule, KogdaIgraMissing, MasterGrantAccess, All };

public record CharacterGroupHeaderDto(CharacterGroupIdentification CharacterGroupId, string Name, bool IsActive, bool IsPublic) : ILinkableWithName
{
    string ILinkableWithName.Name => Name;

    LinkType ILinkable.LinkType => LinkType.ResultCharacterGroup;

    string? ILinkable.Identification => CharacterGroupId.CharacterGroupId.ToString();

    int? ILinkable.ProjectId => CharacterGroupId.ProjectId;
}
