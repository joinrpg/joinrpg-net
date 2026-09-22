using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class MainMenuViewComponent(
    ICurrentUserAccessor currentUserAccessor,
    IProjectRepository projectRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ILogger<MainMenuViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var projectLinks = (await GetProjectLinks()).OrderByDisplayPriority().ToArray();
            string? currentProjectName = null;
            if (HttpContext.TryGetProjectIdFromItems() is ProjectIdentification currentProjectId)
            {
                currentProjectName = await TryGetProjectName(currentProjectId);

                if (currentProjectName?.Length > 30)
                {
                    currentProjectName = currentProjectName.Take(30).AsString() + "...";
                }

            }

            var viewModel = new MainMenuViewModel(projectLinks, currentProjectName);
            return View("MainMenu", viewModel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке данных главного меню");
            return View("MainMenu", new MainMenuViewModel([], null));
        }
    }

    /// <summary>
    /// Название текущего проекта, если он существует.
    /// </summary>
    /// <remarks>
    /// Кажется, будто это лишнее хождение в базу, но оно всегда будет в кеше.
    /// Проекта может не быть вовсе — например, бот пришёл на /2024, получил 404 и мы рисуем
    /// страницу ошибки, а projectId всё ещё лежит в HttpContext.Items. Это штатная ситуация,
    /// ошибкой её логировать не нужно.
    /// </remarks>
    private async Task<string?> TryGetProjectName(ProjectIdentification currentProjectId)
    {
        try
        {
            var info = await projectMetadataRepository.GetProjectMetadata(currentProjectId);
            return info.ProjectName;
        }
        catch (JoinRpgEntityNotFoundException ex)
        {
            logger.LogDebug(ex, "Проект {projectId} не найден, не показываем его название в меню", currentProjectId);
            return null;
        }
    }

    private async Task<ProjectPersonalizedInfo[]> GetProjectLinks()
    {
        var user = currentUserAccessor.UserIdOrDefault;
        if (user == null)
        {
            return [];
        }
        return await projectRepository.GetPersonalizedProjectsBySpecification(ProjectListSpecification.MyActiveProjects(currentUserAccessor.UserIdentification));
    }
}
