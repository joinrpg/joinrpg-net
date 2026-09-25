using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Portal.Menu;
using JoinRpg.WebPortal.Managers.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Portal.Test.Menu;

/// <summary>
/// Заход на несуществующий проект (например, бот пришёл на /2024) — штатная ситуация:
/// отдаём 404, но projectId остаётся в HttpContext.Items и меню всё равно рисуется на странице
/// ошибки. Такое не должно шуметь в error-мониторинге.
/// </summary>
public class MenuViewComponentNotFoundProjectTest
{
    private static readonly ProjectIdentification NotExistingProjectId = new(2024);

    [Fact]
    public async Task ProjectMenu_ProjectNotFound_ShouldNotLogError()
    {
        var logger = new RecordingLogger<ProjectMenuViewComponent>();
        var component = new ProjectMenuViewComponent(
            currentUserAccessor: null!,
            new NotFoundProjectMetadataRepository(),
            new FakeCurrentProjectAccessor(NotExistingProjectId),
            claimsRepository: null!,
            captainRulesRepository: null!,
            logger);

        var result = await component.InvokeAsync();

        _ = result.ShouldBeOfType<ContentViewComponentResult>();
        logger.LoggedLevels.ShouldNotContain(LogLevel.Error);
    }

    [Fact]
    public async Task MainMenu_ProjectNotFound_ShouldNotLogError()
    {
        var logger = new RecordingLogger<MainMenuViewComponent>();
        var component = new MainMenuViewComponent(
            new AnonymousCurrentUserAccessor(),
            projectRepository: null!,
            claimsRepository: null!,
            new NotFoundProjectMetadataRepository(),
            logger)
        {
            ViewComponentContext = CreateViewComponentContext(),
        };

        var result = await component.InvokeAsync();

        _ = result.ShouldBeOfType<ViewViewComponentResult>();
        logger.LoggedLevels.ShouldNotContain(LogLevel.Error);
    }

    private static ViewComponentContext CreateViewComponentContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[Constants.ProjectIdName] = NotExistingProjectId.Value;

        var viewContext = new ViewContext
        {
            HttpContext = httpContext,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
            TempData = new TempDataDictionary(httpContext, new NullTempDataProvider()),
        };

        return new ViewComponentContext { ViewContext = viewContext };
    }

    private sealed class NotFoundProjectMetadataRepository : IProjectMetadataRepository
    {
        public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
            => throw new JoinRpgEntityNotFoundException(projectId.Value, "project");

        public Task<ProjectDetails> GetProjectDetails(ProjectIdentification projectId)
            => throw new JoinRpgEntityNotFoundException(projectId.Value, "project");

        public void PrimeCache(ProjectInfo projectInfo) { }
    }

    private sealed class FakeCurrentProjectAccessor(ProjectIdentification projectId) : ICurrentProjectAccessor
    {
        public ProjectIdentification ProjectId => projectId;
    }

    private sealed class AnonymousCurrentUserAccessor : ICurrentUserAccessor
    {
        public int? UserIdOrDefault => null;
        public UserDisplayName DisplayName => throw new NotSupportedException();
        public bool IsAdmin => false;
        public AvatarIdentification? Avatar => null;
    }

    private sealed class NullTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<LogLevel> LoggedLevels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => LoggedLevels.Add(logLevel);
    }
}
