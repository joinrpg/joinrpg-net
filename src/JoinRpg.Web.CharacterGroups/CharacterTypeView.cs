using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.CharacterGroups
{
    internal enum CharacterTypeView
    {
        [Display(Name = "Игрок", Description = "Персонаж — обычный игрок")]
        Player,
        [Display(Name = "NPC", Description = "На этого персонажа нельзя заявляться, он будет просто отображаться в ролевке")]
        NonPlayer,
        [Display(Name = "Слот персонажей", Description = "Экспериментальная фича. Это прописанный мастерами шаблон, на который можно принять несколько заявок, каждая из которых станет полноценным персонажем.")]
        Slot,
    }
}
