using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
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

        var filter = new CaptureNoAccessExceptionFilter(new FakeProjectMetadataRepository(mock.ProjectInfo));

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

        var filter = new CaptureNoAccessExceptionFilter(new FakeProjectMetadataRepository(mock.ProjectInfo));

        await filter.OnExceptionAsync(context);

        var viewResult = context.Result.ShouldBeOfType<ViewResult>();
        viewResult.ViewName.ShouldBe("ErrorNotActiveProject");
        viewResult.ViewData.ShouldNotBeNull();
        viewResult.ViewData[Constants.ProjectIdName].ShouldBe(mock.ProjectInfo.ProjectId.Value);
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
            => Task.FromResult(new JoinRpg.DomainTypes.ProjectMetadata.ProjectDetails(new MarkdownString(""), [], false));

        public void PrimeCache(ProjectInfo projectInfo) { }
    }
}
