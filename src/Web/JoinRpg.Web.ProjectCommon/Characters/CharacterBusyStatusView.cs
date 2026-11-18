using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectCommon;

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
    [Display(Name = "Шаблон")]
    Slot,
    [Display(Name = "Горячий шаблон")]
    HotSlot,
    [Display(Name = "Неизвестно")]
    Unknown,
    [Display(Name = "Горячая вакансия")]
    HotVacancy,
}
