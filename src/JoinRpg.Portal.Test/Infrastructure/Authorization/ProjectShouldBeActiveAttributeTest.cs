using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
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
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), new object());

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
        var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object>(), new object());

        var filter = new ProjectShouldBeActiveAttribute();

        await Should.ThrowAsync<ProjectDeactivatedException>(() =>
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

    private class FakeProjectMetadataRepository(ProjectInfo projectInfo) : IProjectMetadataRepository
    {
        public Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId, bool ignoreCache = false)
            => Task.FromResult(projectInfo);

        public Task<JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails> GetProjectDetails(ProjectIdentification projectId)
            => Task.FromResult(new JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails(new MarkdownString(), [], false));
    }
}
