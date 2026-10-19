using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure.Authorization;

/// <summary>
/// Проверка статуса проекта до входа в экшен или страницу — веб-зеркало <c>ProjectActiveRequirement</c> (ADR023).
/// Бросает <see cref="ProjectDeactivatedException"/> (для заблокированного — <see cref="ProjectBlockedException"/>),
/// её превращает в страницу ошибки <c>CaptureNoAccessExceptionFilter</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public abstract class ProjectStatusFilterAttribute : ActionFilterAttribute, IAsyncPageFilter
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await CheckProjectStatusAsync(context.HttpContext);
        await next();
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        await CheckProjectStatusAsync(context.HttpContext);
        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    /// <summary>Бросает исключение, если в таком статусе экшен недоступен.</summary>
    protected abstract void EnsureAllowed(ProjectInfo projectInfo);

    private async Task CheckProjectStatusAsync(HttpContext httpContext)
    {
        if (httpContext.TryGetProjectIdFromItems() is not ProjectIdentification projectId)
        {
            return;
        }

        var projectMetadataRepository = httpContext.RequestServices.GetRequiredService<IProjectMetadataRepository>();
        EnsureAllowed(await projectMetadataRepository.GetProjectMetadata(projectId));
    }
}

/// <summary>Только активный проект: не архив и не заблокирован (<c>MustBeActive</c>).</summary>
public class ProjectShouldBeActiveAttribute : ProjectStatusFilterAttribute
{
    protected override void EnsureAllowed(ProjectInfo projectInfo) => _ = projectInfo.EnsureProjectActive();
}

/// <summary>Активный или заблокированный, но не архив (<c>AllowBlocked</c>) — то, что разрешено и на время восстановления.</summary>
public class ProjectShouldNotBeArchivedAttribute : ProjectStatusFilterAttribute
{
    protected override void EnsureAllowed(ProjectInfo projectInfo) => _ = projectInfo.EnsureNotArchived();
}
