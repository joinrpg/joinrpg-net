using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace JoinRpg.Portal.Test.Infrastructure.Authorization;

public class ProjectShouldBeActiveAttributeTest
{
    private static DefaultHttpContext CreateHttpContext(ProjectInfo projectInfo)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items["ProjectId"] = projectInfo.ProjectId.Value;
        httpContext.RequestServices = new TestServiceProvider()
            .WithService<IProjectMetadataRepository>(new FakeProjectMetadataRepository(projectInfo));
        return httpContext;
    }

    [Fact]
    public async Task OnActionExecutionAsync_ActiveProject_ShouldNotThrow()
    {
        var mock = new MockedProject();
        var httpContext = CreateHttpContext(mock.ProjectInfo);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());

        var filter = new ProjectShouldBeActiveAttribute();

        await filter.OnActionExecutionAsync(context, () =>
            Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object())));
    }

    [Fact]
    public async Task OnActionExecutionAsync_InactiveProject_ShouldThrowProjectDeactivatedException()
    {
        var mock = new MockedProject();
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
        var httpContext = CreateHttpContext(mock.ProjectInfo);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());

        var filter = new ProjectShouldBeActiveAttribute();

        await Should.ThrowAsync<ProjectDeactivatedException>(() =>
            filter.OnActionExecutionAsync(context, () =>
                Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()))));
    }

    [Fact]
    public async Task OnActionExecutionAsync_BlockedProject_ShouldThrowProjectBlockedException()
    {
        var mock = new MockedProject();
        mock.Project.IsBlocked = true;
        mock.ReInitProjectInfo();
        var httpContext = CreateHttpContext(mock.ProjectInfo);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());

        var filter = new ProjectShouldBeActiveAttribute();

        await Should.ThrowAsync<ProjectBlockedException>(() =>
            filter.OnActionExecutionAsync(context, () =>
                Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object()))));
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_ActiveProject_ShouldNotThrow()
    {
        var mock = new MockedProject();
        var httpContext = CreateHttpContext(mock.ProjectInfo);
        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new CompiledPageActionDescriptor()));
        var context = new PageHandlerExecutingContext(pageContext, new List<IFilterMetadata>(), new HandlerMethodDescriptor(), new Dictionary<string, object?>(), new object());

        var filter = new ProjectShouldBeActiveAttribute();

        await filter.OnPageHandlerExecutionAsync(context, () =>
            Task.FromResult(new PageHandlerExecutedContext(pageContext, new List<IFilterMetadata>(), new HandlerMethodDescriptor(), new object())));
    }

    [Fact]
    public async Task OnPageHandlerExecutionAsync_InactiveProject_ShouldThrowProjectDeactivatedException()
    {
        var mock = new MockedProject();
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
        var httpContext = CreateHttpContext(mock.ProjectInfo);
        var pageContext = new PageContext(new ActionContext(httpContext, new RouteData(), new CompiledPageActionDescriptor()));
        var context = new PageHandlerExecutingContext(pageContext, new List<IFilterMetadata>(), new HandlerMethodDescriptor(), new Dictionary<string, object?>(), new object());

        var filter = new ProjectShouldBeActiveAttribute();

        await Should.ThrowAsync<ProjectDeactivatedException>(() =>
            filter.OnPageHandlerExecutionAsync(context, () =>
                Task.FromResult(new PageHandlerExecutedContext(pageContext, new List<IFilterMetadata>(), new HandlerMethodDescriptor(), new object()))));
    }

    public enum TestProjectState { Active, Archived, Blocked }

    // ADR023: какой атрибут в каком статусе пропускает экшен. null — пропускает.
    [Theory]
    [InlineData(nameof(ProjectShouldBeActiveAttribute), TestProjectState.Active, null)]
    [InlineData(nameof(ProjectShouldBeActiveAttribute), TestProjectState.Archived, typeof(ProjectDeactivatedException))]
    [InlineData(nameof(ProjectShouldBeActiveAttribute), TestProjectState.Blocked, typeof(ProjectBlockedException))]
    [InlineData(nameof(ProjectShouldNotBeArchivedAttribute), TestProjectState.Active, null)]
    [InlineData(nameof(ProjectShouldNotBeArchivedAttribute), TestProjectState.Archived, typeof(ProjectDeactivatedException))]
    [InlineData(nameof(ProjectShouldNotBeArchivedAttribute), TestProjectState.Blocked, null)]
    public async Task Attribute_MatchesProjectState(string attributeName, TestProjectState state, Type? expectedException)
    {
        var mock = new MockedProject();
        switch (state)
        {
            case TestProjectState.Archived:
                mock.Project.Active = false;
                mock.Project.IsAcceptingClaims = false;
                break;
            case TestProjectState.Blocked:
                mock.Project.IsBlocked = true;
                break;
        }
        mock.ReInitProjectInfo();
        var httpContext = CreateHttpContext(mock.ProjectInfo);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), new object());
        ProjectStatusFilterAttribute filter = attributeName == nameof(ProjectShouldBeActiveAttribute)
            ? new ProjectShouldBeActiveAttribute()
            : new ProjectShouldNotBeArchivedAttribute();

        var act = () => filter.OnActionExecutionAsync(context, () =>
            Task.FromResult(new ActionExecutedContext(actionContext, new List<IFilterMetadata>(), new object())));

        if (expectedException is null)
        {
            await act();
        }
        else
        {
            (await Should.ThrowAsync<ProjectDeactivatedException>(act)).GetType().ShouldBe(expectedException);
        }
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
