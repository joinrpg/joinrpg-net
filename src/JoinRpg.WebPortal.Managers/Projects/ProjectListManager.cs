using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Games.Projects;
using JoinRpg.Web.Models;

namespace JoinRpg.WebPortal.Managers.Projects;

/// <summary>
/// Shows project list
/// </summary>
public class ProjectListManager(IProjectRepository projectRepository, ICurrentUserAccessor currentUser)
{
    public async Task<HomeViewModel> LoadModel(bool showInactive = false, int maxProjects = int.MaxValue)
    {
        var allProjects = await projectRepository.GetProjectsBySpecification(currentUser.UserIdentificationOrDefault,
            showInactive ? ProjectListSpecification.All : ProjectListSpecification.Active);

        var projects =
            allProjects
                .Where(p => (showInactive && p.ActiveClaimsCount > 0) || p.HasMyMasterAccess || p.HasMyClaims || p.IsAcceptingClaims)
                .OrderByDisplayPriority()
                .ToList();

        var alwaysShowProjects = allProjects.Where(p => p.HasMyMasterAccess || p.HasMyClaims).OrderByDisplayPriority().ToList();

        var finalProjects = alwaysShowProjects.UnionUntilTotalCount(projects, maxProjects).Select(p => new ProjectListItemViewModel(p)).ToList();

        return new HomeViewModel
        {
            ActiveProjects = finalProjects,
            HasMoreProjects = projects.Count > finalProjects.Count,
        };
    }
}
