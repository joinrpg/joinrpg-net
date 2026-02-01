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
            await projectRepository.GetProjectsBySpecification(currentUser.UserIdentificationOrDefault,
            showInactive ? ProjectListSpecification.MyAllProjects : ProjectListSpecification.MyActiveProjects)
            : [];

        var allProjects = await projectRepository.GetProjectsBySpecification(currentUser.UserIdentificationOrDefault,
            showInactive ? ProjectListSpecification.AllPublic : ProjectListSpecification.ActivePublic);

        return new HomeViewModel
        {
            MyProjects = [.. myProjects.Select(p => new ProjectListItemViewModel(p))],
            AllProjects = [.. allProjects.Select(p => new ProjectListItemViewModel(p))],
            HasMoreProjects = false,
        };
    }

    public async Task<HomeViewModel> LoadHomeModel(int maxProjects)
    {
        var myProjects = currentUser.UserIdentificationOrDefault is UserIdentification userId
            ? await projectRepository.GetProjectsBySpecification(userId, ProjectListSpecification.MyActiveProjects) : [];

        var allProjects = await projectRepository.GetProjectsBySpecification(currentUser.UserIdentificationOrDefault, ProjectListSpecification.ActivePublic);

        var projects =
            allProjects
                .Except(myProjects)
                .Where(p => p.IsAcceptingClaims)
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
