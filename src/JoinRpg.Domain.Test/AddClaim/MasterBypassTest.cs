using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain.Test.AddClaim;

/// <summary>
/// Мастер вправе обойти часть причин запрета: закрытый приём заявок и незаполненные контакты
/// игрока. Всё остальное остаётся запретом и для него.
/// </summary>
public class MasterBypassTest
{
    private MockedProject Mock { get; } = new MockedProject();

    private ProjectInfo ClaimsClosed => Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);

    private ProjectInfo RequiringPhone => Mock.ProjectInfo.WithProfileRequirementSettings(
        ProjectProfileRequirementSettings.AllNotRequired with { RequirePhone = MandatoryStatus.Required });

    private UserInfo PlayerWithoutPhone => Mock.PlayerInfo with { PhoneNumber = null };

    [Fact]
    public void MasterCanInviteWhenClaimsClosed()
        => Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, ClaimsClosed, ClaimOperation.AddByMaster)
            .ShouldBeEmpty();

    [Fact]
    public void PlayerStillCantSendClaimWhenClaimsClosed()
        => Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, ClaimsClosed, ClaimOperation.AddByPlayer).Kinds()
            .ShouldBe([AddClaimForbideReason.ProjectClaimsClosed]);

    [Fact]
    public void MasterCanInvitePlayerWithoutRequiredContacts()
        => Mock.Character.ValidateIfCanAddClaim(PlayerWithoutPhone, RequiringPhone, ClaimOperation.AddByMaster)
            .ShouldBeEmpty();

    [Fact]
    public void PlayerStillCantSendClaimWithoutRequiredContacts()
        => Mock.Character.ValidateIfCanAddClaim(PlayerWithoutPhone, RequiringPhone, ClaimOperation.AddByPlayer).Kinds()
            .ShouldBe([AddClaimForbideReason.PhoneMissing]);

    /// <summary>
    /// Ключевой тест: обход мастером убирает только обходимую причину, а не выключает проверки
    /// целиком. Если фильтр обхода применить после вытеснения по фатальности, фатальный
    /// <see cref="AddClaimForbideReason.ProjectClaimsClosed"/> сначала вытеснит
    /// <see cref="AddClaimForbideReason.Busy"/>, а потом уйдёт сам — и мастер сможет пригласить
    /// игрока на уже занятую роль.
    /// </summary>
    [Fact]
    public void MasterInvitingIntoClosedProjectStillSeesNonOverridableReasons()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Master);

        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, ClaimsClosed, ClaimOperation.AddByMaster).Kinds()
            .ShouldBe([AddClaimForbideReason.Busy]);
    }

    [Fact]
    public void MasterCantInviteIntoArchivedProject()
    {
        var archived = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.Archived);

        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, archived, ClaimOperation.AddByMaster).Kinds()
            .ShouldBe([AddClaimForbideReason.ProjectNotActive]);
    }

    // См. https://github.com/joinrpg/joinrpg-net/issues/4743 — пока это осознанно запрет.
    [Fact]
    public void MasterCantInviteIntoNpc()
    {
        Mock.Character.CharacterType = CharacterType.NonPlayer;

        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, Mock.ProjectInfo, ClaimOperation.AddByMaster).Kinds()
            .ShouldBe([AddClaimForbideReason.Npc]);
    }

    [Fact]
    public void MasterCantInviteIntoBusyCharacter()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Master);

        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, Mock.ProjectInfo, ClaimOperation.AddByMaster).Kinds()
            .ShouldBe([AddClaimForbideReason.Busy]);
    }

    [Fact]
    public void MasterCantInviteTwice()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);

        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, Mock.ProjectInfo, ClaimOperation.AddByMaster).Kinds()
            .ShouldBe([AddClaimForbideReason.AlreadySent]);
    }

    /// <summary>
    /// Показ считается глазами игрока, кто бы страницу ни открыл: мастеру нельзя показывать, что
    /// закрытый проект принимает заявки.
    /// </summary>
    [Fact]
    public void DisplayNeverBypasses()
        => Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, ClaimsClosed, ClaimOperation.DisplayForPlayer).Kinds()
            .ShouldBe([AddClaimForbideReason.ProjectClaimsClosed]);

    [Fact]
    public void IsAcceptingClaimsNeverBypasses()
        => Mock.Character.IsAcceptingClaims(ClaimsClosed).ShouldBeFalse();
}
