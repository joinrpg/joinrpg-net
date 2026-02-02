using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Portal.Models;
using JoinRpg.PrimitiveTypes;
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
        string? currentProjectName = null;
        if (HttpContext.TryGetProjectIdFromItems() is ProjectIdentification currentProjectId)
        {
            // Кажется, будто это лишнее хождение в базу, но оно всегда будет в кеше
            var info = await projectMetadataRepository.GetProjectMetadata(currentProjectId);

            currentProjectName = info.ProjectName;

            if (currentProjectName.Length > 30)
            {
                currentProjectName = currentProjectName.Take(30).AsString() + "...";
            }

        }

        var viewModel = new MainMenuViewModel(projectLinks, currentProjectName);
        return View("MainMenu", viewModel);
    }

    private async Task<ProjectPersonalizedInfo[]> GetProjectLinks()
    {
        var user = currentUserAccessor.UserIdOrDefault;
        if (user == null)
        {
            return [];
        }
        return await projectRepository.GetPersonalizedProjectsBySpecification(currentUserAccessor.UserIdentification, ProjectListSpecification.MyActiveProjects);
    }
}
