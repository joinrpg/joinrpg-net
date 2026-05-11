using Bunit;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Tests.ProjectRolesList;

public class ProjectRolesListComponentTest : TestContext
{
    private readonly FakeProjectRolesListClient _client = new();
    private readonly ProjectIdentification _projectId = new(123);

    public ProjectRolesListComponentTest()
    {
        Services.AddSingleton<IProjectRolesListClient>(_client);
        Services.AddSingleton<ILogger<ProjectRolesListGrid>>(new NullLogger<ProjectRolesListGrid>());
    }

    [Fact(Skip = "Need to fix loading message test")]
    public void ShouldRenderLoadingMessageWhenModelNull()
    {
        // Arrange
        _client.NextGetListResult = null; // will cause OnInitializedAsync to set _model null

        // Act
        var cut = Render<ProjectRolesListGrid>(
            parameters => parameters.Add(p => p.ProjectId, _projectId));

        // Assert
        cut.Find("div.alert").ShouldNotBeNull();
    }

    [Fact]
    public void ShouldRenderEmptyListWhenNoItems()
    {
        // Arrange
        _client.NextGetListResult = new ProjectRolesListViewModel([], HasEditAccess: true);

        // Act
        var cut = Render<ProjectRolesListGrid>(
            parameters => parameters.Add(p => p.ProjectId, _projectId));

        // Assert
        cut.Find("div.panel").ShouldNotBeNull();
        cut.FindAll("div.rule-list-row").ShouldBeEmpty();
        cut.Markup.ShouldContain("Сетки ролей");
    }

    [Fact]
    public void ShouldRenderAddButtonWhenHasEditAccess()
    {
        // Arrange
        _client.NextGetListResult = new ProjectRolesListViewModel([], HasEditAccess: true);

        // Act
        var cut = Render<ProjectRolesListGrid>(
            parameters => parameters.Add(p => p.ProjectId, _projectId));

        // Assert
        var addButton = cut.Find("button.btn");
        addButton.ShouldNotBeNull();
        addButton.HasAttribute("disabled").ShouldBeFalse();
        addButton.TextContent.ShouldContain("Добавить сетку");
    }

    [Fact]
    public void ShouldRenderDisabledAddButtonWhenNoEditAccess()
    {
        // Arrange
        _client.NextGetListResult = new ProjectRolesListViewModel([], HasEditAccess: false);

        // Act
        var cut = Render<ProjectRolesListGrid>(
            parameters => parameters.Add(p => p.ProjectId, _projectId));

        // Assert
        var addButton = cut.Find("button.btn");
        addButton.ShouldNotBeNull();
        addButton.HasAttribute("disabled").ShouldBeTrue();
        addButton.TextContent.ShouldContain("Добавить сетку");
    }

    private class FakeProjectRolesListClient : IProjectRolesListClient
    {
        public ProjectRolesListViewModel? NextGetListResult { get; set; }
        public ProjectRolesListViewModel? NextCreateResult { get; set; }
        public ProjectRolesListViewModel? NextUpdateResult { get; set; }

        public Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId)
        {
            return Task.FromResult(NextGetListResult ?? new ProjectRolesListViewModel([], HasEditAccess: true));
        }

        public Task<ProjectRolesListViewModel> Create(ProjectIdentification projectId, AddProjectRolesListViewModel model)
        {
            return Task.FromResult(NextCreateResult ?? new ProjectRolesListViewModel([], HasEditAccess: true));
        }

        public Task<ProjectRolesListViewModel> Update(DomainTypes.ProjectMetadata.ProjectRolesList model)
        {
            return Task.FromResult(NextUpdateResult ?? new ProjectRolesListViewModel([], HasEditAccess: true));
        }

        public Task Remove(JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesListIdentification projectRolesListId)
        {
            return Task.CompletedTask;
        }
    }
}
