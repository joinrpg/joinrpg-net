using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using JoinRpg.Web.ProjectCommon.Fields;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ProjectRolesList;

public class ProjectRolesListEditFormTest : BunitContext
{
    private readonly ProjectIdentification projectId = new(123);
    private readonly FailingProjectRolesListClient client = new();

    public ProjectRolesListEditFormTest()
    {
        Services.AddSingleton<IProjectRolesListClient>(client);
        Services.AddSingleton<ICharacterGroupsClient>(new EmptyCharacterGroupsClient());
        Services.AddSingleton<IProjectFieldsClient>(new EmptyFieldsClient());
        _ = Services.AddJoinRpgWebComponents();
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        JSInterop.Mode = JSRuntimeMode.Loose;
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    [Fact]
    public void ShouldShowServerRefusal()
    {
        client.FailWith = new ServerProblemException("Проект заблокирован, изменения запрещены");
        var cut = RenderForm();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Проект заблокирован, изменения запрещены"));
    }

    [Fact]
    public void ShouldNotShowMessageOfUnexpectedException()
    {
        client.FailWith = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");
        var cut = RenderForm();

        cut.Find("form").Submit();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось сохранить сетку ролей"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }

    private IRenderedComponent<ProjectRolesListEditForm> RenderForm()
        => Render<ProjectRolesListEditForm>(parameters => parameters
            .Add(p => p.ProjectId, projectId)
            .Add(p => p.Model, new EditProjectRolesListViewModel { Name = "Все роли" }));

    private class FailingProjectRolesListClient : IProjectRolesListClient
    {
        public Exception FailWith { get; set; } = new InvalidOperationException();

        public Task<ProjectRolesListViewModel> Create(ProjectIdentification projectId, AddProjectRolesListViewModel model)
            => Task.FromException<ProjectRolesListViewModel>(FailWith);

        public Task<ProjectRolesListViewModel> Update(DomainTypes.ProjectMetadata.ProjectRolesList model)
            => Task.FromException<ProjectRolesListViewModel>(FailWith);

        public Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId) => throw new NotImplementedException();

        public Task<DomainTypes.ProjectMetadata.ProjectRolesList> GetById(ProjectRolesListIdentification id)
            => throw new NotImplementedException();

        public Task Remove(ProjectRolesListIdentification projectRolesListId) => throw new NotImplementedException();

        public Task<ProjectRolesListViewModel> SetDefault(ProjectRolesListIdentification id) => throw new NotImplementedException();
    }

    private class EmptyCharacterGroupsClient : ICharacterGroupsClient
    {
        public Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId) => Task.FromResult(new List<CharacterGroupDto>());

        public Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId) => Task.FromResult(new List<CharacterGroupDto>());

        public Task<List<CharacterGroupDto>> GetValidParentGroups(CharacterGroupIdentification groupId)
            => Task.FromResult(new List<CharacterGroupDto>());
    }

    private class EmptyFieldsClient : IProjectFieldsClient
    {
        public Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId) => Task.FromResult(new List<ProjectFieldDto>());
    }
}
