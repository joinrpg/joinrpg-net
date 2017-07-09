using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.Characters
{
  public enum CharacterBusyStatusView
  {
    [Display(Name = "Занят")]
    HasPlayer,
    [Display(Name = "Обсуждается")]
    Discussed,
    [Display(Name = "Нет заявок")]
    NotSend,
    [Display(Name = "NPC")]
    Npc,
  }
}