using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Menu;

/// <summary>
/// Текст для &lt;title&gt;: «Заголовок страницы — Название проекта — JoinRpg».
/// Страницы проекта название в свой заголовок не вписывают — его добавляет этот компонент.
/// </summary>
public class PageTitleViewComponent(
    IProjectMetadataRepository projectMetadataRepository,
    ILogger<PageTitleViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var projectName = await TryGetProjectName();
        // Content кодирует текст сам
        return Content(BuildTitle(ViewData["Title"] as string, projectName));
    }

    public static string BuildTitle(string? pageTitle, string? projectName)
        => string.Join(" — ", new[] { pageTitle, projectName, "JoinRpg" }.Where(part => !string.IsNullOrWhiteSpace(part)));

    private async Task<string?> TryGetProjectName()
    {
        if (HttpContext.TryGetProjectIdFromItems() is not ProjectIdentification projectId)
        {
            return null;
        }
        try
        {
            // Метаданные кешируются на запрос, меню проекта их всё равно загружает
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
            return projectInfo.ProjectName.Value;
        }
        catch (JoinRpgEntityNotFoundException)
        {
            // Несуществующий проект (страница 404) — заголовок без названия
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Ошибка при загрузке названия проекта для заголовка страницы");
            return null;
        }
    }
}
