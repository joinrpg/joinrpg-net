using JoinRpg.Web.ProjectCommon.Masters;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.ProjectCommon;

/// <summary>
/// Данные для панели «нет доступа к проекту» (<see cref="JoinNoAccessToProjectPanel"/>).
/// Сериализуется на клиент, поэтому без domain-сущностей.
/// </summary>
public record NoAccessToProjectViewModel(
    ProjectIdentification ProjectId,
    string ProjectName,
    // null ⇔ доступа к проекту нет вообще; иначе — требуемое право (которого у пользователя нет).
    PermissionBadgeViewModel? RequiredPermission,
    IReadOnlyList<UserLinkViewModel> CanGrantAccess) : IProjectLinkViewModel;
