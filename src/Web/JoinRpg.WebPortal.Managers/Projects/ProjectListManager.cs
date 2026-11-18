using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.Games.Projects;
using JoinRpg.WebPortal.Models;

namespace JoinRpg.WebPortal.Managers.Projects;

/// <summary>
/// Shows project list
/// </summary>
public class ProjectListManager(IProjectRepository projectRepository, ICurrentUserAccessor currentUser)
{
    public async Task<HomeViewModel> LoadModel(bool showInactive = false)
    {
        var myProjects =
            currentUser.UserIdentificationOrDefault is UserIdentification userId ?
            await projectRepository.GetPersonalizedProjectsBySpecification(
            showInactive ? ProjectListSpecification.MyAllProjects(userId) : ProjectListSpecification.MyActiveProjects(userId))
            : [];

        var allProjects = await projectRepository.GetProjectsBySpecification(showInactive ? ProjectListSpecification.AllPublic : ProjectListSpecification.ActivePublic);
        var myProjectIds = myProjects.Select(x => x.ProjectId).ToList();

        return new HomeViewModel
        {
            MyProjects = [.. myProjects.Select(p => new ProjectListItemViewModel(p))],
            AllProjects = [.. allProjects
                .Where(p => !myProjectIds.Contains(p.ProjectId))
                .OrderByDescending(p => p.ActiveClaimsCount)
                .Select(p => new ProjectListItemViewModel(p))],
            HasMoreProjects = false,
        };
    }

    public async Task<HomeViewModel> LoadHomeModel(int maxProjects)
    {
        var myProjects = currentUser.UserIdentificationOrDefault is UserIdentification userId
            ? await projectRepository.GetPersonalizedProjectsBySpecification(ProjectListSpecification.MyActiveProjects(userId)) : [];

        var allProjects = await projectRepository.GetProjectsBySpecification(ProjectListSpecification.ActivePublic);

        var myProjectIds = myProjects.Select(x => x.ProjectId).ToList();

        var projects =
            allProjects
                .Where(p => !myProjectIds.Contains(p.ProjectId))
                .OrderByDescending(p => p.ActiveClaimsCount)
                .Take(maxProjects + 1)
                .Select(p => new ProjectListItemViewModel(p))
                .ToList();

        return new HomeViewModel
        {
            MyProjects = [.. myProjects.Select(p => new ProjectListItemViewModel(p))],
            AllProjects = [.. projects.Take(maxProjects)],
            HasMoreProjects = projects.Count > maxProjects,
        };
    }
}
