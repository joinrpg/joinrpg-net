using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Models;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class MainMenuViewComponent(
    ICurrentUserAccessor currentUserAccessor,
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var projectLinks = (await GetProjectLinks()).OrderByDisplayPriority().ToArray();
        int? currentProjectId = ViewBag.ProjectId is int x ? x : null;
        string? currentProjectName = null;
        if (currentProjectId is not null)
        {
            var currentProject = projectLinks.SingleOrDefault(p => p.ProjectId == currentProjectId);
            if (currentProject is null)
            {
                var info = await projectMetadataRepository.GetProjectMetadata(new(currentProjectId.Value));
                currentProjectName = info.ProjectName;
            }
            else
            {
                currentProjectName = currentProject.ProjectName;
            }


            if (currentProjectName.Length > 30)
            {
                currentProjectName = currentProjectName.Take(30).AsString() + "...";
            }

        }

        var viewModel = new MainMenuViewModel()
        {
            ProjectLinks = projectLinks,
            CurrentProjectId = currentProjectId,
            CurrentProjectName = currentProjectName,
        };
        return View("MainMenu", viewModel);
    }

    private async Task<ProjectShortInfo[]> GetProjectLinks()
    {
        var user = currentUserAccessor.UserIdOrDefault;
        if (user == null)
        {
            return [];
        }
        return await projectRepository.GetProjectsBySpecification(currentUserAccessor.UserIdentification, ProjectListSpecification.MyActiveProjects);
    }
}
