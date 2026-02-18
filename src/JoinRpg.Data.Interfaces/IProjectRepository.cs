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
    Task<ProjectPersonalizedInfo[]> GetPersonalizedProjectsBySpecification(PersonalizedProjectListSpecification projectListSpecification);

    /// <summary>
    /// Без учета данных о доступе к проектам и наличия заявки, более быстрый метод
    /// </summary>
    Task<ProjectShortInfo[]> GetProjectsBySpecification(ProjectListSpecification projectListSpecification);

    /// <summary>
    /// Проекты грузятся всегда относительно какого-то пользователя.
    /// Даже в тех местах, где речь не идет про доступ — нужно всегда сортировать «мои» проекты вперед
    /// </summary>
    Task<ProjectPersonalizedInfo[]> GetProjectsByIds(UserIdentification? userId, ProjectIdentification[] ids);

    Task<CharacterGroupHeaderDto[]> LoadDirectChildGroupHeaders(CharacterGroupIdentification characterGroupId);

    Task<CharacterGroupHeaderDto[]> GetGroupHeaders(IReadOnlyCollection<CharacterGroupIdentification> characterGroupIds);
}

public record ProjectListSpecification(ProjectListCriteria Criteria, bool LoadArchived)
{

    public static ProjectListSpecification ActiveProjectsWithSchedule { get; } = new ProjectListSpecification(ProjectListCriteria.HasSchedule, LoadArchived: false);

    public static ProjectListSpecification ActiveProjectsWithoutKogdaIgra { get; } = new ProjectListSpecification(ProjectListCriteria.KogdaIgraMissing, LoadArchived: false);

    public static ProjectListSpecification All { get; } = new ProjectListSpecification(ProjectListCriteria.All, LoadArchived: true);

    public static ProjectListSpecification Active { get; } = new ProjectListSpecification(ProjectListCriteria.All, LoadArchived: false);

    public static ProjectListSpecification ActivePublic { get; } = new ProjectListSpecification(ProjectListCriteria.Public, LoadArchived: false);

    public static ProjectListSpecification AllPublic { get; } = new ProjectListSpecification(ProjectListCriteria.Public, LoadArchived: true);
    public static PersonalizedProjectListSpecification AllProjectsWithMasterAccess(UserIdentification userId)
    => new(ProjectListCriteria.MasterAccess, LoadArchived: true, userId);

    public static PersonalizedProjectListSpecification ActiveProjectsWithGrantMasterAccess(UserIdentification userId)
        => new(ProjectListCriteria.MasterGrantAccess, LoadArchived: false, userId);
    public static PersonalizedProjectListSpecification MyActiveProjects(UserIdentification userId)
        => new(ProjectListCriteria.MasterOrActiveClaim, LoadArchived: false, userId);

    public static PersonalizedProjectListSpecification MyAllProjects(UserIdentification userId)
        => new(ProjectListCriteria.MasterOrActiveClaim, LoadArchived: true, userId);
    public static PersonalizedProjectListSpecification ForCloning(UserIdentification userId)
        => new(ProjectListCriteria.ForCloning, LoadArchived: true, userId);
    public static PersonalizedProjectListSpecification ActiveWithMyMasterAccess(UserIdentification userId)
        => new(ProjectListCriteria.MasterAccess, LoadArchived: false, userId);
}

public record PersonalizedProjectListSpecification(ProjectListCriteria Criteria, bool LoadArchived, UserIdentification UserId)
    : ProjectListSpecification(Criteria, LoadArchived)
{

}

public enum ProjectListCriteria { MasterAccess, MasterOrActiveClaim, ForCloning, HasSchedule, KogdaIgraMissing, MasterGrantAccess, All, Public };

public record CharacterGroupHeaderDto(CharacterGroupIdentification CharacterGroupId, string Name, bool IsActive, bool IsPublic) : ILinkableWithName
{
    string ILinkableWithName.Name => Name;

    LinkType ILinkable.LinkType => LinkType.ResultCharacterGroup;

    string? ILinkable.Identification => CharacterGroupId.CharacterGroupId.ToString();

    int? ILinkable.ProjectId => CharacterGroupId.ProjectId;
}
