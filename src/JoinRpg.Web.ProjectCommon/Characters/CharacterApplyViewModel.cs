namespace JoinRpg.Web.ProjectCommon;

public record CharacterApplyViewModel(
    CharacterIdentification CharacterId,
    CharacterBusyStatusView BusyStatus,
    int? SlotCount,
    bool IsHot,
    bool IsSlot);
