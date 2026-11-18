using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace JoinRpg.Portal.Test.Infrastructure;

public class CaptureNoAccessExceptionFilterTest
{
    private static DefaultHttpContext CreateHttpContext(ProjectInfo projectInfo)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[Constants.ProjectIdName] = projectInfo.ProjectId.Value;
        httpContext.RequestServices = new TestServiceProvider()
            .WithService<IProjectMetadataRepository>(new FakeProjectMetadataRepository(projectInfo));
        return httpContext;
    }

    [Fact]
    public async Task OnExceptionAsync_NoAccessToProjectException_ShouldSetProjectIdInViewData()
    {
        var mock = new MockedProject();
        var httpContext = CreateHttpContext(mock.ProjectInfo);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new NoAccessToProjectException(mock.ProjectInfo, userId: null),
        };

        var filter = new CaptureNoAccessExceptionFilter();

        await filter.OnExceptionAsync(context);

        var viewResult = context.Result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ErrorNoAccessToProject");
        viewResult.ViewData.ShouldNotBeNull();
        viewResult.ViewData[Constants.ProjectIdName].ShouldBe(mock.ProjectInfo.ProjectId.Value);
        viewResult.Model.ShouldBeOfType<NoAccessToProjectViewModel>();
    }

    [Fact]
    public async Task OnExceptionAsync_ProjectDeactivatedException_ShouldSetProjectIdInViewData()
    {
        var mock = new MockedProject();
        var httpContext = CreateHttpContext(mock.ProjectInfo);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = new ProjectDeactivatedException(mock.ProjectInfo.ProjectId),
        };

        var filter = new CaptureNoAccessExceptionFilter();

        await filter.OnExceptionAsync(context);

        var viewResult = context.Result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ErrorNotActiveProject");
        viewResult.ViewData.ShouldNotBeNull();
        viewResult.ViewData[Constants.ProjectIdName].ShouldBe(mock.ProjectInfo.ProjectId.Value);
    }

    private static PageHandlerExecutingContext CreatePageContext(HttpContext httpContext)
    {
        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new CompiledPageActionDescriptor()));
        return new PageHandlerExecutingContext(pageContext, [], new HandlerMethodDescriptor(), new Dictionary<string, object?>(), new object());
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_NoAccessToProjectException_ReturnsNoAccessView()
    {
        var mock = new MockedProject();
        var context = CreatePageContext(CreateHttpContext(mock.ProjectInfo));
        PageHandlerExecutionDelegate next = () => Task.FromException<PageHandlerExecutedContext>(
            new NoAccessToProjectException(mock.ProjectInfo, userId: null));

        await new CaptureNoAccessExceptionFilter().OnPageHandlerExecutionAsync(context, next);

        var viewResult = context.Result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ErrorNoAccessToProject");
        viewResult.ViewData[Constants.ProjectIdName].ShouldBe(mock.ProjectInfo.ProjectId.Value);
        viewResult.Model.ShouldBeOfType<NoAccessToProjectViewModel>();
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_ProjectBlockedException_ReturnsBlockedView()
    {
        var mock = new MockedProject();
        var context = CreatePageContext(CreateHttpContext(mock.ProjectInfo));
        PageHandlerExecutionDelegate next = () => Task.FromException<PageHandlerExecutedContext>(
            new ProjectBlockedException(mock.ProjectInfo.ProjectId));

        await new CaptureNoAccessExceptionFilter().OnPageHandlerExecutionAsync(context, next);

        var viewResult = context.Result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ErrorNotActiveProject");
        viewResult.ViewData["ProjectBlocked"].ShouldBe(true);
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_OtherException_Rethrows()
    {
        var mock = new MockedProject();
        var context = CreatePageContext(CreateHttpContext(mock.ProjectInfo));
        PageHandlerExecutionDelegate next = () => Task.FromException<PageHandlerExecutedContext>(
            new InvalidOperationException("boom"));

        _ = await Should.ThrowAsync<InvalidOperationException>(
            () => new CaptureNoAccessExceptionFilter().OnPageHandlerExecutionAsync(context, next));
        context.Result.ShouldBeNull();
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_WithoutException_PassesThrough()
    {
        var mock = new MockedProject();
        var pageContext = new PageContext(new ActionContext(
            CreateHttpContext(mock.ProjectInfo), new RouteData(), new CompiledPageActionDescriptor()));
        var context = new PageHandlerExecutingContext(pageContext, [], new HandlerMethodDescriptor(), new Dictionary<string, object?>(), new object());

        await new CaptureNoAccessExceptionFilter().OnPageHandlerExecutionAsync(
            context,
            () => Task.FromResult(new PageHandlerExecutedContext(pageContext, [], context.HandlerMethod, new object())));

        context.Result.ShouldBeNull();
    }

    private class TestServiceProvider : IServiceProvider
    {
        private readonly Dictionary<Type, object> services = new();

        public TestServiceProvider WithService<T>(T service)
        {
            services[typeof(T)] = service!;
            return this;
        }

        public object? GetService(Type serviceType) => services.TryGetValue(serviceType, out var service) ? service : null;
    }
}
