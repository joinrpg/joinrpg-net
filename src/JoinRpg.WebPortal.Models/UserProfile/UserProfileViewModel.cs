using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.Models.ClaimList;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.Models;

public class UserProfileViewModel
{
    public required string DisplayName { get; set; }

    /// <summary>
    /// Активные проекты, где у пользователя есть доступ мастера — используется, чтобы не предлагать
    /// повторно выдать доступ в <see cref="ProjectsToAdd"/>. Архивные проекты сюда не входят: их и так
    /// нет в <see cref="CanGrantAccessProjects"/> (доступ выдаётся только в активные проекты).
    /// </summary>
    public required IReadOnlyCollection<ProjectLinkViewModel> ThisUserProjects { get; set; }

    public required IReadOnlyCollection<ProjectListItemViewModel> ActiveMasterProjects { get; set; }

    public required IReadOnlyCollection<ProjectLinkViewModel> ArchivedProjects { get; set; }

    [ReadOnly(true)]
    public IEnumerable<ProjectLinkViewModel> CanGrantAccessProjects { get; set; } = [];

    [ReadOnly(true)]
    public IEnumerable<ProjectLinkViewModel> CanInviteProjects { get; set; } = [];

    public int UserId { get; set; }

    public IEnumerable<ProjectLinkViewModel> ProjectsToAdd => CanGrantAccessProjects.Except(ThisUserProjects);

    [ReadOnly(true)]
    public MyClaimListViewModel? Claims { get; set; }

    public required UserProfileDetailsViewModel Details { get; set; }

    [ReadOnly(true)]
    public bool HasAdminAccess => Admin is not null;

    public required UserAdminOperationsViewModel? Admin { get; set; }
}

public record class UserAdminOperationsViewModel(Uri LogLink);
