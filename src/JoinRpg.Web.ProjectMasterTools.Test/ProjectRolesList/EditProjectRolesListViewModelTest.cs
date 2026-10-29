using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.ProjectMasterTools.Test.ProjectRolesList;

public class EditProjectRolesListViewModelTest
{
    // Варианты в форме упорядочены иначе, чем числа в БД, поэтому соответствие задано явно.
    // Ошибка в нём молча переключила бы режим колонки «Игрок» у сохранённой сетки.
    [Theory]
    [InlineData(PlayerColumnMode.None)]
    [InlineData(PlayerColumnMode.NameOnly)]
    [InlineData(PlayerColumnMode.PublicOnly)]
    [InlineData(PlayerColumnMode.All)]
    public void PlayerColumn_SurvivesRoundTripThroughForm(PlayerColumnMode mode)
    {
        var projectId = new ProjectIdentification(123);
        var domain = new DomainTypes.ProjectMetadata.ProjectRolesList(
            new ProjectRolesListIdentification(projectId, 1),
            "Сетка",
            CharacterGroupId: null,
            PublicMode: false,
            Fields: [],
            ContactsColumn: mode,
            GroupsColumn: ProjectRolesListVisibilityMode.None,
            GroupsViewMode: RolesGridGroupsViewMode.None,
            ShowRolesFilter: ShowRolesFilter.All);

        EditProjectRolesListViewModel.FromDomain(domain).ToDomain().ContactsColumn.ShouldBe(mode);
    }

    [Theory]
    [InlineData(PlayerAvatarMode.None)]
    [InlineData(PlayerAvatarMode.Small)]
    [InlineData(PlayerAvatarMode.Large)]
    public void PlayerAvatar_SurvivesRoundTripThroughForm(PlayerAvatarMode mode)
    {
        var projectId = new ProjectIdentification(123);
        var domain = new DomainTypes.ProjectMetadata.ProjectRolesList(
            new ProjectRolesListIdentification(projectId, 1),
            "Сетка",
            CharacterGroupId: null,
            PublicMode: false,
            Fields: [],
            ContactsColumn: PlayerColumnMode.NameOnly,
            GroupsColumn: ProjectRolesListVisibilityMode.None,
            GroupsViewMode: RolesGridGroupsViewMode.None,
            ShowRolesFilter: ShowRolesFilter.All,
            PlayerAvatar: mode);

        EditProjectRolesListViewModel.FromDomain(domain).ToDomain().PlayerAvatar.ShouldBe(mode);
    }

    [Fact]
    public void NewRolesList_NoAvatarsByDefault()
        => new AddProjectRolesListViewModel().ToDomain(new ProjectIdentification(123))
            .PlayerAvatar.ShouldBe(PlayerAvatarMode.None);

    [Fact]
    public void Avatar_WithoutPlayerColumn_IsInvalid()
    {
        var model = new AddProjectRolesListViewModel
        {
            Name = "Сетка",
            ContactsColumn = PlayerColumnModeView.None,
            PlayerAvatar = PlayerAvatarModeView.Small,
        };

        model.Validate(new System.ComponentModel.DataAnnotations.ValidationContext(model))
            .ShouldContain(r => r.MemberNames.Contains(nameof(AddProjectRolesListViewModel.PlayerAvatar)));
    }

    [Fact]
    public void NewRolesList_ShowsPlayerNameByDefault()
        => new AddProjectRolesListViewModel().ToDomain(new ProjectIdentification(123))
            .ContactsColumn.ShouldBe(PlayerColumnMode.NameOnly);
}
