using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.Models.Characters;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// «Подать заявку» считается глазами игрока, а «Пригласить игрока» — по мастерским правилам:
/// мастер вправе пригласить туда, куда игрок сам не заявится.
/// </summary>
public class CharacterNavigationViewModelTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private CharacterNavigationViewModel Create(Character character)
        => CharacterNavigationViewModel.FromCharacter(
            Mock.GetCharacterInfo(character),
            CharacterNavigationPage.Character,
            Mock.Master.GetId());

    [Fact]
    public void OrdinaryCharacter_PlayerCanApply_MasterCanInvite()
    {
        var vm = Create(Mock.Character);

        vm.CanAddClaim.ShouldBeTrue();
        vm.CanInvitePlayer.ShouldBeTrue();
    }

    // Роль «только по приглашению» (#3303).
    [Fact]
    public void InvitationOnlyCharacter_PlayerCantApply_MasterCanInvite()
    {
        Mock.Character.IsAcceptingClaims = false;

        var vm = Create(Mock.Character);

        vm.CanAddClaim.ShouldBeFalse();
        vm.CanInvitePlayer.ShouldBeTrue();
    }

    [Fact]
    public void ClaimsClosed_PlayerCantApply_MasterCanInvite()
    {
        Mock.Project.IsAcceptingClaims = false;
        Mock.ReInitProjectInfo();

        var vm = Create(Mock.Character);

        vm.CanAddClaim.ShouldBeFalse();
        vm.CanInvitePlayer.ShouldBeTrue();
    }

    [Fact]
    public void BusyCharacter_NobodyCanApplyOrInvite()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

        var vm = Create(Mock.Character);

        vm.CanAddClaim.ShouldBeFalse();
        vm.CanInvitePlayer.ShouldBeFalse();
    }

    [Fact]
    public void Npc_NobodyCanApplyOrInvite()
    {
        Mock.Character.CharacterType = CharacterType.NonPlayer;

        var vm = Create(Mock.Character);

        vm.CanAddClaim.ShouldBeFalse();
        vm.CanInvitePlayer.ShouldBeFalse();
    }
}
