using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.WebPortal.Models.Test;

public class CharacterGroupListViewModelTest
{
    private readonly MockedProject mock = new();

    private CharacterGroup RootGroup => mock.Project.CharacterGroups.Single(g => g.IsRoot);

    [Fact]
    public void AnonymousUserGetsPublicGroups()
    {
        // Эндпоинт встраивания списка ролей (/{projectId}/roles/{groupId}/indexjson) помечен
        // AllowAnonymous, поэтому currentUserId здесь штатно равен null.
        RootGroup.IsPublic = true;

        var groups = CharacterGroupListViewModel.GetGroups(RootGroup, currentUserId: null, mock.ProjectInfo);

        groups.ShouldHaveSingleItem().CharacterGroupId.ShouldBe(RootGroup.CharacterGroupId);
    }

    [Fact]
    public void AnonymousUserDoesNotGetPrivateGroups()
    {
        RootGroup.IsPublic = false;

        var groups = CharacterGroupListViewModel.GetGroups(RootGroup, currentUserId: null, mock.ProjectInfo);

        groups.ShouldBeEmpty();
    }

    [Fact]
    public void MasterGetsPrivateGroups()
    {
        RootGroup.IsPublic = false;

        var groups = CharacterGroupListViewModel.GetGroups(RootGroup, new UserIdentification(mock.Master.UserId), mock.ProjectInfo);

        groups.ShouldHaveSingleItem().CharacterGroupId.ShouldBe(RootGroup.CharacterGroupId);
    }

    [Fact]
    public void AnonymousUserGetsPublicCharacters()
    {
        RootGroup.IsPublic = true;
        mock.Character.IsPublic = true;

        var groups = CharacterGroupListViewModel.GetGroups(RootGroup, currentUserId: null, mock.ProjectInfo);

        groups.ShouldHaveSingleItem()
            .ActiveCharacters.ShouldHaveSingleItem()
            .CharacterId.ShouldBe(mock.Character.CharacterId);
    }
}
