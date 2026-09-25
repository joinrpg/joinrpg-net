using JoinRpg.Common.WebComponents;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Models.Test.Characters;

/// <summary>
/// Дерево ролей для публичного JSON (<c>GameGroups/indexjson</c> и <c>hotjson</c>): персонажи в нём
/// считаются поверх доменного агрегата (ADR013), дерево групп пока приходит из EF.
/// </summary>
public class CharacterGroupListViewModelTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private CharacterGroup RootGroup => Mock.Project.CharacterGroups.Single(group => group.IsRoot);

    private CharacterGroupInfo Root => Mock.ProjectInfo.GetGroupById(RootGroup.CharacterGroupId);

    private UserIdentification MasterId => new(Mock.Master.UserId);

    /// <summary>Посторонний: не мастер проекта и не игрок ни одной заявки.</summary>
    private static UserIdentification StrangerId => new(12345);

    /// <summary>
    /// Открывает корневую группу: иначе постороннему не видно всё дерево целиком, и проверять
    /// видимость самого персонажа не на чем.
    /// </summary>
    private void MakeRootGroupPublic()
    {
        RootGroup.IsPublic = true;
        Mock.ReInitProjectInfo();
    }

    private IReadOnlyCollection<CharacterGroupListItemViewModel> GetGroups(UserIdentification? currentUserId)
        => [.. CharacterGroupListViewModel
            .GetGroups(
                Root,
                [.. Mock.Project.Characters.Select(Mock.GetCharacterInfo)],
                new Dictionary<DomainTypes.CharacterGroupIdentification, CharacterGroupFullInfo>(),
                currentUserId,
                Mock.ProjectInfo)];

    private IReadOnlyCollection<CharacterViewModel> GetCharacters(UserIdentification? currentUserId)
        => [.. GetGroups(currentUserId).SelectMany(group => group.ActiveCharacters)];

    private CharacterViewModel GetSingleCharacter(UserIdentification? currentUserId)
        => GetCharacters(currentUserId).Single(character => character.CharacterId == Mock.Character.CharacterId);

    /// <summary>
    /// Эндпоинт встраивания списка ролей помечен AllowAnonymous, поэтому currentUserId здесь
    /// штатно равен null — на этом виджет уже падал с 500 (см. #4869).
    /// </summary>
    [Fact]
    public void AnonymousUserGetsPublicGroups()
    {
        MakeRootGroupPublic();

        GetGroups(currentUserId: null).ShouldHaveSingleItem().CharacterGroupId.ShouldBe(RootGroup.CharacterGroupId);
    }

    [Fact]
    public void AnonymousUserDoesNotGetPrivateGroups()
    {
        RootGroup.IsPublic = false;

        GetGroups(currentUserId: null).ShouldBeEmpty();
    }

    [Fact]
    public void MasterGetsPrivateGroups()
    {
        RootGroup.IsPublic = false;

        GetGroups(MasterId).ShouldHaveSingleItem().CharacterGroupId.ShouldBe(RootGroup.CharacterGroupId);
    }

    [Fact]
    public void AnonymousUserGetsPublicCharacters()
    {
        MakeRootGroupPublic();
        Mock.Character.IsPublic = true;

        GetCharacters(currentUserId: null).ShouldHaveSingleItem().CharacterId.ShouldBe(Mock.Character.CharacterId);
    }

    [Fact]
    public void CharacterOfRootGroupIsListed()
    {
        var character = GetSingleCharacter(MasterId);

        character.CharacterName.ShouldBe(Mock.Character.CharacterName);
        character.IsFirstCopy.ShouldBeTrue();
        character.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.Vacancy);
        character.ApplyStatus.IsAvailable.ShouldBeTrue();
        character.PlayerLink.ShouldBeNull();
        character.ActiveClaimsCount.ShouldBe(0);
    }

    /// <summary>
    /// Ради этого доступность и переехала на общие правила: раньше она выводилась из BusyStatus и
    /// про статус проекта не знала (issue #4766).
    /// </summary>
    [Fact]
    public void ClaimsClosedMakesCharacterUnavailable()
    {
        Mock.Project.IsAcceptingClaims = false;
        Mock.ReInitProjectInfo();
        Mock.ProjectInfo.ProjectStatus.ShouldBe(ProjectLifecycleStatus.ActiveClaimsClosed);

        GetSingleCharacter(MasterId).ApplyStatus.IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void ApprovedClaimGivesPlayerLinkAndBusyStatus()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

        var character = GetSingleCharacter(MasterId);

        character.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.HasPlayer);
        character.ApplyStatus.IsAvailable.ShouldBeFalse();
        character.PlayerLink!.UserId.ShouldBe(new UserIdentification(Mock.Player.UserId));
        character.ActiveClaimsCount.ShouldBe(1);
    }

    /// <summary>
    /// Публичный персонаж со скрытым игроком остаётся в списке — прячется только игрок.
    /// (В <c>CharacterVisibility</c> это <c>PlayerHidden</c>, а не <c>Public</c>, поэтому легко
    /// случайно вычеркнуть такого персонажа из публичного JSON целиком.)
    /// </summary>
    [Fact]
    public void HiddenPlayerIsNotShownToStranger()
    {
        MakeRootGroupPublic();
        Mock.Character.IsPublic = true;
        Mock.Character.HidePlayerForCharacter = true;
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

        GetSingleCharacter(StrangerId).PlayerLink!.ViewMode.ShouldBe(ViewMode.Hide);
        GetSingleCharacter(MasterId).PlayerLink!.ViewMode.ShouldBe(ViewMode.ShowAsPrivate);
    }

    [Fact]
    public void ActiveClaimMakesCharacterDiscussed()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);

        var character = GetSingleCharacter(MasterId);

        character.ApplyStatus.BusyStatus.ShouldBe(CharacterBusyStatusView.Discussed);
        // Заявки есть, но ни одна не принята — заявиться всё ещё можно.
        character.ApplyStatus.IsAvailable.ShouldBeTrue();
        character.ActiveClaimsCount.ShouldBe(1);
    }

    [Fact]
    public void DeletedCharacterIsNotListed()
    {
        Mock.Character.IsActive = false;

        GetCharacters(MasterId).ShouldBeEmpty();
    }

    /// <summary>Непубличного персонажа видит мастер, но не посторонний.</summary>
    [Fact]
    public void PrivateCharacterIsVisibleToMasterOnly()
    {
        MakeRootGroupPublic();
        Mock.Character.IsPublic = false;

        GetCharacters(MasterId).ShouldNotBeEmpty();
        GetCharacters(StrangerId).ShouldBeEmpty();
    }

    [Fact]
    public void PublicCharacterIsVisibleToStranger()
    {
        MakeRootGroupPublic();
        Mock.Character.IsPublic = true;

        GetCharacters(StrangerId).ShouldNotBeEmpty();
    }

    [Fact]
    public void CharacterOfAnotherGroupIsNotListedInRoot()
    {
        var other = Mock.CreateCharacter("другая группа");
        other.ParentCharacterGroupIds = [Mock.Group.CharacterGroupId];

        GetCharacters(MasterId).Select(character => character.CharacterId).ShouldBe([Mock.Character.CharacterId]);
    }
}
