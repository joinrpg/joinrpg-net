namespace JoinRpg.Web.ProjectCommon;

public record CharacterApplyViewModel(
    CharacterIdentification CharacterId,
    CharacterBusyStatusView BusyStatus,
    int? SlotCount,
    bool IsHot)
{
    public bool IsSlot => BusyStatus is CharacterBusyStatusView.Slot or CharacterBusyStatusView.HotSlot;

    // null SlotCount у слота — не «мест нет», а безлимитный шаблон (см. IsAcceptingClaims в JoinRpg.Domain).
    // Discussed — есть поданные заявки, но мастер ещё не одобрил ни одну: заявиться ещё можно.
    public bool IsAvailable => BusyStatus is CharacterBusyStatusView.Vacancy or CharacterBusyStatusView.HotVacancy or CharacterBusyStatusView.Discussed
        || (IsSlot && SlotCount is null or > 0);
}
