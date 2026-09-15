namespace JoinRpg.Web.ProjectCommon;

/// <param name="IsAvailable">
/// Можно ли игроку подать заявку на этого персонажа. Считается доменными правилами
/// (<c>ClaimValidator.IsAvailableForPlayer</c>), а не выводится из <paramref name="BusyStatus"/>:
/// тот описывает состояние самого персонажа и про статус проекта ничего не знает, поэтому в
/// проекте с закрытым приёмом заявок кнопка «Заявиться» показывалась и вела на форму, где подать
/// заявку нельзя (см. issue #4766).
/// </param>
public record CharacterApplyViewModel(
    CharacterIdentification CharacterId,
    CharacterBusyStatusView BusyStatus,
    int? SlotCount,
    bool IsHot,
    bool IsAvailable)
{
    public bool IsSlot => BusyStatus is CharacterBusyStatusView.Slot or CharacterBusyStatusView.HotSlot;
}
