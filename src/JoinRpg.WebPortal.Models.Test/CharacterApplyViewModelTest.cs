using JoinRpg.DomainTypes;
using JoinRpg.Web.ProjectCommon;

namespace JoinRpg.WebPortal.Models.Test;

public class CharacterApplyViewModelTest
{
    private static CharacterApplyViewModel CreateSlot(int? slotCount) => new(
        new CharacterIdentification(new ProjectIdentification(1), 1),
        CharacterBusyStatusView.Slot,
        slotCount,
        IsHot: false);

    private static CharacterApplyViewModel CreatePlayer(CharacterBusyStatusView busyStatus) => new(
        new CharacterIdentification(new ProjectIdentification(1), 1),
        busyStatus,
        SlotCount: null,
        IsHot: false);

    [Fact]
    public void UnlimitedSlot_IsAvailable()
    {
        CreateSlot(null).IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void SlotWithFreePlaces_IsAvailable()
    {
        CreateSlot(3).IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void ExhaustedSlot_IsNotAvailable()
    {
        CreateSlot(0).IsAvailable.ShouldBeFalse();
    }

    [Fact]
    public void DiscussedCharacter_IsAvailable()
    {
        // Есть поданные, но не одобренные заявки — мастер ещё не выбрал игрока, заявиться можно.
        CreatePlayer(CharacterBusyStatusView.Discussed).IsAvailable.ShouldBeTrue();
    }

    [Fact]
    public void CharacterWithPlayer_IsNotAvailable()
    {
        CreatePlayer(CharacterBusyStatusView.HasPlayer).IsAvailable.ShouldBeFalse();
    }
}
