using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Claims;

public enum CharacterBusyStatusView
{
    [Display(Name = "Занят")]
    HasPlayer,
    [Display(Name = "Обсуждается")]
    Discussed,
    [Display(Name = "Нет заявок")]
    Vacancy,
    [Display(Name = "NPC")]
    Npc,
    Slot,
    HotSlot,
    Unknown,
    HotVacancy,
}
