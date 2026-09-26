using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces;

public interface IProjectRepository : IDisposable
{
    [Obsolete("Используйте IProjectMetadataRepository.GetProjectMetadata")]
    Task<Project> GetProjectAsync(int project);
    Task<Project?> GetProjectWithFieldsAsync(int project);

    /// <summary>
    /// Отдаёт EF-граф проекта, который нужен <c>JoinrpgMarkdownLinkRenderer</c> для директив
    /// вида <c>%персонаж</c>, <c>%группа</c>, <c>%список</c>: персонажей проекта с их заявками
    /// и группы. Без него рендеринг грузит их лениво, по одному на директиву.
    /// </summary>
    /// <remarks>
    /// Метод существует только потому, что рендерер markdown завязан на EF-сущность <c>Project</c>,
    /// и уйдёт вместе с этой завязкой — см. #4923. Звать его нужно там и только там, где текст
    /// действительно рендерится: страницам, которым нужны лишь таргеты или заголовки, граф не нужен.
    ///
    /// Раньше эту загрузку молча делал <c>IPlotRepository.GetPlotFolderAsync</c> — из-за чего её
    /// платили и те страницы сюжетов, которым она не нужна.
    /// </remarks>
    Task<Project> GetProjectForMarkdownRendering(ProjectIdentification projectId);

    Task<CharacterGroup?> GetGroupAsync(CharacterGroupIdentification characterGroupId);

    Task<CharacterGroup?> LoadGroupWithTreeAsync(int projectId, int? characterGroupId = null);

    Task<IList<CharacterGroup>> LoadGroups(IReadOnlyCollection<CharacterGroupIdentification> groupIds);

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

    /// <summary>
    /// Публичные проекты, открытые для рекламы горячих ролей (принимают заявки) и имеющие хотя бы одну
    /// горячую роль, с их <see cref="ProjectAdvertisementCandidate.ActiveClaimsCount"/> для взвешивания
    /// по ADR010 §4.
    /// </summary>
    Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenForHotRoleAdvertisement();

    /// <summary>
    /// Публичные проекты, принимающие заявки, у которых самая ранняя заявка подана не позднее недели
    /// назад, либо заявок ещё не было вовсе (эвристика для "недавно открывшихся" проектов —
    /// настоящей даты открытия приёма заявок пока нигде не хранится).
    /// </summary>
    Task<IReadOnlyCollection<ProjectAdvertisementCandidate>> GetPublicProjectsOpenedForClaimsInLastWeek();
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

    public static PersonalizedProjectListSpecification ActiveProjectsWithManageClaimsAccess(UserIdentification userId)
        => new(ProjectListCriteria.MasterManageClaimsAccess, LoadArchived: false, userId);
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

public enum ProjectListCriteria { MasterAccess, MasterOrActiveClaim, ForCloning, HasSchedule, KogdaIgraMissing, MasterGrantAccess, MasterManageClaimsAccess, All, Public };
