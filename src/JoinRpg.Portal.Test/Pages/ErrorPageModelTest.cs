using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Pages.Error;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Portal.Test.Pages;

/// <summary>
/// Страница ошибки не пишет Error на ссылку на несуществующий проект и показывает «Проект не найден» (#5709).
/// </summary>
public class ErrorPageModelTest
{
    [Fact]
    public void ProjectNotFound_LoggedAsInformation()
    {
        var logger = new RecordingLogger();

        var model = Execute(logger, new ProjectNotFoundException(new ProjectIdentification(2024)), StatusCodes.Status404NotFound);

        model.IsProjectNotFound.ShouldBeTrue();
        model.HttpStatusCode.ShouldBe(StatusCodes.Status404NotFound);
        logger.Levels.ShouldBe([LogLevel.Information]);
    }

    [Fact]
    public void OtherException_LoggedAsError()
    {
        var logger = new RecordingLogger();

        var model = Execute(logger, new InvalidOperationException("Падение для теста"), StatusCodes.Status500InternalServerError);

        model.IsProjectNotFound.ShouldBeFalse();
        logger.Levels.ShouldBe([LogLevel.Error]);
    }

    private static ErrorPageModel Execute(RecordingLogger logger, Exception exception, int statusCode)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.StatusCode = statusCode;
        httpContext.Features.Set<IExceptionHandlerPathFeature>(new ExceptionHandlerFeature
        {
            Error = exception,
            Path = "/2024/roles",
        });

        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new CompiledPageActionDescriptor()));
        var model = new ErrorPageModel(logger) { PageContext = pageContext };

        model.OnPageHandlerExecuting(new PageHandlerExecutingContext(pageContext, [], handlerMethod: null, new Dictionary<string, object?>(), model));
        return model;
    }

    private sealed class RecordingLogger : ILogger<ErrorPageModel>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Levels.Add(logLevel);
    }
}
