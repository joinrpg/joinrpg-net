using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Test.AddClaim;

/// <summary>
/// «Показывать ли кнопку „Заявиться“» — раньше это выводилось из <c>BusyStatus</c> прямо во
/// view-модели и про статус проекта не знало (issue #4766). Теперь ответ считают общие правила.
/// </summary>
public class IsAvailableForPlayerTest
{
    private MockedProject Mock { get; } = new MockedProject();

    [Fact]
    public void OrdinaryCharacterIsAvailable()
        => Mock.Character.IsAvailableForPlayer(Mock.ProjectInfo).ShouldBeTrue();

    [Fact]
    public void UnlimitedSlotIsAvailable()
        => CreateSlot(slotLimit: null).IsAvailableForPlayer(Mock.ProjectInfo).ShouldBeTrue();

    [Fact]
    public void SlotWithFreePlacesIsAvailable()
        => CreateSlot(slotLimit: 3).IsAvailableForPlayer(Mock.ProjectInfo).ShouldBeTrue();

    [Fact]
    public void ExhaustedSlotIsNotAvailable()
        => CreateSlot(slotLimit: 0).IsAvailableForPlayer(Mock.ProjectInfo).ShouldBeFalse();

    /// <summary>
    /// Есть поданные, но не одобренные заявки — мастер ещё не выбрал игрока, заявиться можно.
    /// </summary>
    [Fact]
    public void DiscussedCharacterIsAvailable()
    {
        _ = Mock.CreateClaim(Mock.Character, Mock.Player);

        Mock.Character.IsAvailableForPlayer(Mock.ProjectInfo).ShouldBeTrue();
    }

    [Fact]
    public void CharacterWithPlayerIsNotAvailable()
    {
        _ = Mock.CreateApprovedClaim(Mock.Character, Mock.Player);

        Mock.Character.IsAvailableForPlayer(Mock.ProjectInfo).ShouldBeFalse();
    }

    [Fact]
    public void NpcIsNotAvailable()
    {
        Mock.Character.CharacterType = CharacterType.NonPlayer;

        Mock.Character.IsAvailableForPlayer(Mock.ProjectInfo).ShouldBeFalse();
    }

    [Fact]
    public void InactiveCharacterIsNotAvailable()
    {
        Mock.Character.IsActive = false;

        Mock.Character.IsAvailableForPlayer(Mock.ProjectInfo).ShouldBeFalse();
    }

    // Главное, ради чего это переехало на правила: раньше кнопка показывалась и вела на форму,
    // где заявку подать нельзя.
    [Fact]
    public void ClaimsClosedMakesCharacterUnavailable()
        => Mock.Character
            .IsAvailableForPlayer(Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed))
            .ShouldBeFalse();

    [Fact]
    public void ArchivedProjectMakesCharacterUnavailable()
        => Mock.Character
            .IsAvailableForPlayer(Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.Archived))
            .ShouldBeFalse();

    /// <summary>
    /// Мастерских послаблений тут нет: страница показывает положение дел глазами игрока, кто бы
    /// её ни открыл. Иначе мастер видел бы кнопку в закрытом проекте.
    /// </summary>
    [Fact]
    public void ClaimsClosedIsNotOverridableEvenThoughMasterCanBypassIt()
    {
        var projectInfo = Mock.ProjectInfo.WithChangedStatus(ProjectLifecycleStatus.ActiveClaimsClosed);

        ClaimForbiddenReason.For(AddClaimForbideReason.ProjectClaimsClosed).MasterCanOverride.ShouldBeTrue();
        Mock.Character.IsAvailableForPlayer(projectInfo).ShouldBeFalse();
    }

    /// <summary>
    /// Перегрузка поверх агрегата отвечает то же самое: ProjectInfo она берёт из самого персонажа.
    /// </summary>
    [Fact]
    public void OverloadOverAggregateAnswersTheSame()
    {
        var character = Mock.GetCharacterInfo(Mock.Character);

        ClaimValidator.IsAvailableForPlayer(character).ShouldBe(Mock.Character.IsAvailableForPlayer(Mock.ProjectInfo));
        ClaimValidator.IsAvailableForPlayer(character).ShouldBeTrue();
    }

    [Fact]
    public void OverloadOverAggregateSeesClosedProject()
    {
        Mock.Project.IsAcceptingClaims = false;
        Mock.ReInitProjectInfo();

        ClaimValidator.IsAvailableForPlayer(Mock.GetCharacterInfo(Mock.Character)).ShouldBeFalse();
    }

    private Character CreateSlot(int? slotLimit)
    {
        var slot = Mock.CreateCharacter("slot");
        slot.CharacterType = CharacterType.Slot;
        slot.CharacterSlotLimit = slotLimit;
        return slot;
    }
}
