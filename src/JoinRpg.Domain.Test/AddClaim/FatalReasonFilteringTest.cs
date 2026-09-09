using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.AddClaim;

/// <summary>
/// Фатальная причина (проект в архиве или не принимает заявки) вытесняет остальные: пока проект
/// в таком состоянии, показывать игроку «роль занята» или «заполните телефон» бессмысленно.
/// Раньше это делалось через <c>yield break</c> прямо в правилах.
/// </summary>
public class FatalReasonFilteringTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void FatalReasonHidesOtherReasons()
    {
        // Персонаж занят — сам по себе это нефатальная причина...
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Master);
        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, Mock.ProjectInfo).Kinds()
            .ShouldContain(AddClaimForbideReason.Busy);

        // ...но если проект вдобавок не принимает заявки, остаётся только это.
        var closedProject = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);

        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, closedProject).Kinds()
            .ShouldBe([AddClaimForbideReason.ProjectClaimsClosed]);
    }

    [Fact]
    public void ArchivedProjectHidesOtherReasons()
    {
        Mock.Character.CharacterType = CharacterType.NonPlayer;
        var archived = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.Archived);

        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, archived).Kinds()
            .ShouldBe([AddClaimForbideReason.ProjectNotActive]);
    }

    [Fact]
    public void NonFatalReasonsAreReportedTogether()
    {
        Mock.Character.CharacterType = CharacterType.NonPlayer;
        Mock.Character.IsActive = false;

        Mock.Character.ValidateIfCanAddClaim(Mock.PlayerInfo, Mock.ProjectInfo).Kinds()
            .ShouldBe(
                [
                    AddClaimForbideReason.CharacterInactive,
                    AddClaimForbideReason.Npc,
                ],
                ignoreOrder: true);
    }
}
