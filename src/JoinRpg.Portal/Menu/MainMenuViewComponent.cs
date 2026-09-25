using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Portal.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

public class MainMenuViewComponent(
    ICurrentUserAccessor currentUserAccessor,
    IProjectRepository projectRepository,
    IClaimsRepository claimsRepository,
    IProjectMetadataRepository projectMetadataRepository,
    ILogger<MainMenuViewComponent> logger) : ViewComponent
{
    /// <summary>
    /// Сколько строк каждого вида показываем в меню. Для проектов дальше идет ссылка на полный
    /// список, для заявок — ничего: остальные видны в профиле, куда ведет «Архив».
    /// </summary>
    private const int MaxRowsInMenu = 20;

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var isAuthenticated = currentUserAccessor.UserIdOrDefault is not null;
        try
        {
            var (projectLinks, claims) = await GetMenuItems();

            string? currentProjectName = null;
            if (HttpContext.TryGetProjectIdFromItems() is ProjectIdentification currentProjectId)
            {
                currentProjectName = await TryGetProjectName(currentProjectId);

                if (currentProjectName?.Length > 30)
                {
                    currentProjectName = currentProjectName.Take(30).AsString() + "...";
                }

            }

            var viewModel = new MainMenuViewModel(
                [.. projectLinks.Take(MaxRowsInMenu)],
                [.. claims.Take(MaxRowsInMenu)],
                HasMoreProjects: projectLinks.Length > MaxRowsInMenu,
                isAuthenticated,
                currentProjectName);
            return View("MainMenu", viewModel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при загрузке данных главного меню");
            return View("MainMenu", MainMenuViewModel.Empty(isAuthenticated));
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

    /// <summary>
    /// Проекты показываем те, где пользователь мастер. Игроцкая часть — это не проекты, а конкретные
    /// заявки: иначе при нескольких заявках в одном проекте непонятно, куда вести ссылку.
    /// </summary>
    private async Task<(ProjectPersonalizedInfo[] Projects, MyClaimShortInfo[] Claims)> GetMenuItems()
    {
        if (currentUserAccessor.UserIdOrDefault is null)
        {
            return ([], []);
        }

        var userId = currentUserAccessor.UserIdentification;

        var projects = await projectRepository.GetPersonalizedProjectsBySpecification(
            ProjectListSpecification.ActiveWithMyMasterAccess(userId));
        var claims = await claimsRepository.GetMyActiveClaimsInActiveProjects(userId);

        return (
            [.. projects.OrderByDisplayPriority()],
            [.. claims.OrderBy(c => c.ProjectName.Value).ThenBy(c => c.CharacterName)]);
    }
}
