using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ProjectRolesList;

public class ProjectRolesListComponentTest : BunitContext
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
        var addButton = cut.Find("button.join-btn");
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
        var addButton = cut.Find("button.join-btn");
        addButton.ShouldNotBeNull();
        addButton.HasAttribute("disabled").ShouldBeTrue();
        addButton.TextContent.ShouldContain("Добавить сетку");
    }

    [Fact]
    public void ShouldShowServerRefusalAndRestoreRowWhenRemoveRefused()
    {
        _client.FailWith = new ServerProblemException("Проект заблокирован, изменения запрещены");
        var cut = RenderWithOneList();

        ClickButton(cut, "Удалить сетку");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Проект заблокирован, изменения запрещены"));
        cut.FindAll("div.rule-list-row").Count.ShouldBe(1);
    }

    [Fact]
    public void ShouldShowGenericTextAndRestoreRowWhenRemoveFailsUnexpectedly()
    {
        _client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderWithOneList();

        ClickButton(cut, "Удалить сетку");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось удалить сетку ролей"));
        cut.Markup.ShouldNotContain("Bad Gateway");
        cut.FindAll("div.rule-list-row").Count.ShouldBe(1);
    }

    [Fact]
    public void ShouldShowServerRefusalWhenSetDefaultRefused()
    {
        _client.FailWith = new ServerProblemException("Недостаточно прав для этого действия");
        var cut = RenderWithOneList();

        ClickButton(cut, "Сделать сеткой ролей по умолчанию");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Недостаточно прав для этого действия"));
    }

    [Fact]
    public void ShouldShowGenericTextWhenSetDefaultFailsUnexpectedly()
    {
        _client.FailWith = new InvalidOperationException("Sequence contains no elements");
        var cut = RenderWithOneList();

        ClickButton(cut, "Сделать сеткой ролей по умолчанию");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось сделать сетку ролей сеткой по умолчанию"));
        cut.Markup.ShouldNotContain("Sequence contains no elements");
    }

    private IRenderedComponent<ProjectRolesListGrid> RenderWithOneList()
    {
        var rolesList = new DomainTypes.ProjectMetadata.ProjectRolesList(
            _projectId, "Все роли", fields: [], DomainTypes.ProjectMetadata.RolesGridGroupsViewMode.None)
        {
            ProjectRolesListId = new(_projectId, 7),
        };
        var model = new ProjectRolesListViewModel(
            [new ProjectRolesListItemViewModel(rolesList, CharacterGroup: null, IsDefault: false)],
            HasEditAccess: true);
        return Render<ProjectRolesListGrid>(parameters => parameters
            .Add(p => p.ProjectId, _projectId)
            .Add(p => p.InitialModel, model));
    }

    private static void ClickButton(IRenderedComponent<ProjectRolesListGrid> cut, string label)
        => cut.FindAll("button").First(b => b.TextContent.Contains(label)).Click();

    private class FakeProjectRolesListClient : IProjectRolesListClient
    {
        public Exception? FailWith { get; set; }
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

        public Task<DomainTypes.ProjectMetadata.ProjectRolesList> GetById(JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesListIdentification id)
        {
            throw new NotImplementedException();
        }

        public Task Remove(JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesListIdentification projectRolesListId)
        {
            return FailWith is null ? Task.CompletedTask : Task.FromException(FailWith);
        }

        public Task<ProjectRolesListViewModel> SetDefault(JoinRpg.DomainTypes.ProjectMetadata.ProjectRolesListIdentification id)
        {
            return FailWith is null
                ? Task.FromResult(NextGetListResult ?? new ProjectRolesListViewModel([], HasEditAccess: true))
                : Task.FromException<ProjectRolesListViewModel>(FailWith);
        }
    }
}
