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
    /// Проекта может не быть вовсе: DiscoverProjectMiddleware только разбирает первый сегмент
    /// пути и кладёт число в HttpContext.Items, в базу он не ходит и существование проекта не
    /// проверяет (см. комментарий к самому middleware). Поэтому на запрос бота вида /2024 мы
    /// отдаём 404, а на странице ошибки в Items лежит projectId несуществующего проекта.
    /// Это штатная ситуация, ошибкой её логировать не нужно.
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
