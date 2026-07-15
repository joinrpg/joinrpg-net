using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.Interfaces;
using JoinRpg.WebPortal.Managers.ProjectMasterTools.ProjectRolesLists;

namespace JoinRpg.WebPortal.Managers.Test.ProjectMasterTools;

public class ProjectRolesListViewModelBuilderTests
{
    private readonly FakeCurrentUserAccessor _currentUserAccessor = new();
    private readonly ProjectInfo _projectInfo;

    public ProjectRolesListViewModelBuilderTests()
    {
        // Используем MockedProject для создания ProjectInfo
        var mock = new MockedProject();
        _projectInfo = mock.ProjectInfo;
    }

    [Fact]
    public void Build_EmptyDomainItems_ReturnsEmptyList()
    {
        var result = ProjectRolesListViewModelBuilder.Build(
                    Array.Empty<ProjectRolesList>(),
                    _projectInfo,
                    _currentUserAccessor);

        result.Items.ShouldBeEmpty();
        result.HasEditAccess.ShouldBeFalse();
    }

    [Fact]
    public void Build_UserHasEditAccess_ReturnsTrue()
    {
        _currentUserAccessor.UserIdentification = new UserIdentification(2);

        var domainItems = new[]
        {
            new ProjectRolesList(
                new ProjectRolesListIdentification(new ProjectIdentification(1), 1),
                "Role 1",
                null,
                PublicMode: true,
                [],
                ProjectRolesListVisibilityMode.All,
                ProjectRolesListVisibilityMode.All,
                GroupsViewMode: RolesGridGroupsViewMode.None,
                ShowRolesFilter: ShowRolesFilter.All),
        };

        var result = ProjectRolesListViewModelBuilder.Build(
            domainItems,
            _projectInfo,
            _currentUserAccessor);

        result.HasEditAccess.ShouldBeTrue();
    }

    [Fact]
    public void Build_UserNoEditAccess_ReturnsFalse()
    {
        _currentUserAccessor.UserIdentification = new UserIdentification(999);

        var domainItems = new[]
                {
            new ProjectRolesList(
                new ProjectRolesListIdentification(new ProjectIdentification(1), 1),
                "Role 1",
                null,
                PublicMode: true,
                [],
                ProjectRolesListVisibilityMode.All,
                ProjectRolesListVisibilityMode.All,
                GroupsViewMode: RolesGridGroupsViewMode.None,
                ShowRolesFilter: ShowRolesFilter.All),
        };

        var result = ProjectRolesListViewModelBuilder.Build(
            domainItems,
            _projectInfo,
            _currentUserAccessor);

        result.HasEditAccess.ShouldBeFalse();
    }

    [Fact]
    public void Build_ProjectInactive_ReturnsFalse()
    {
        _currentUserAccessor.UserIdentification = new UserIdentification(2);
        var mock = new MockedProject();
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();

        var result = ProjectRolesListViewModelBuilder.Build(
            Array.Empty<ProjectRolesList>(),
            mock.ProjectInfo,
            _currentUserAccessor);

        result.HasEditAccess.ShouldBeFalse();
    }

    [Fact]
    public void Build_WithCharacterGroupNames_MapsGroupName()
    {
        var groupId = new CharacterGroupIdentification(new ProjectIdentification(1), 10);
        var domainItem = new ProjectRolesList(
            new ProjectRolesListIdentification(new ProjectIdentification(1), 1),
            "Role 1",
            groupId,
            PublicMode: true,
            [],
            ProjectRolesListVisibilityMode.All,
            ProjectRolesListVisibilityMode.All,
            GroupsViewMode: RolesGridGroupsViewMode.None,
            ShowRolesFilter: ShowRolesFilter.All);

        var characterGroupNames = new Dictionary<CharacterGroupIdentification, string>
        {
            [groupId] = "Test Group",
        };

        var result = ProjectRolesListViewModelBuilder.Build(
            [domainItem],
            _projectInfo,
            _currentUserAccessor,
            characterGroupNames);

        var item = result.Items.ShouldHaveSingleItem();
        item.CharacterGroupName.ShouldBe("Test Group");
    }

    [Fact]
    public void Build_WithoutCharacterGroupNames_GroupNameNull()
    {
        var groupId = new CharacterGroupIdentification(new ProjectIdentification(1), 10);
        var domainItem = new ProjectRolesList(
            new ProjectRolesListIdentification(new ProjectIdentification(1), 1),
            "Role 1",
            groupId,
            PublicMode: true,
            [],
            ProjectRolesListVisibilityMode.All,
            ProjectRolesListVisibilityMode.All,
            GroupsViewMode: RolesGridGroupsViewMode.None,
            ShowRolesFilter: ShowRolesFilter.All);

        var result = ProjectRolesListViewModelBuilder.Build(
            [domainItem],
            _projectInfo,
            _currentUserAccessor,
            characterGroupNames: null);

        var item = result.Items.ShouldHaveSingleItem();
        item.CharacterGroupName.ShouldBeNull();
    }

    private class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        public UserIdentification UserIdentification { get; set; } = new UserIdentification(0);
        public int? UserIdOrDefault => UserIdentification.Value;
        public UserDisplayName DisplayName => new UserDisplayName("Test User", null);
        public bool IsAdmin => false;
        public AvatarIdentification? Avatar => null;
    }
}
