using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.CharacterGroups
{
    internal enum CharacterTypeView
    {
        [Display(Name = "Игрок", Description = "Персонаж — обычный игрок")]
        Player,
        [Display(Name = "NPC", Description = "На этого персонажа нельзя заявляться, он будет просто отображаться в ролевке")]
        NonPlayer,
    }
}
