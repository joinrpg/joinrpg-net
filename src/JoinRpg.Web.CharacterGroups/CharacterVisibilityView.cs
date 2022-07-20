using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.CharacterGroups;

internal enum CharacterVisibilityView
{
    [Display(Name = "Публично", Description = "Персонаж, его имя, публичные поля и игрок видны всем")]
    Public,
    [Display(Name = "Скрыть игрока", Description = "Игрок скрыт, но персонаж, его имя и публичные поля видны всем")]
    PlayerHidden,
    [Display(Name = "Скрытый персонаж", Description = "Персонаж виден только мастерам. Игрок не сможет заявиться, но мастера смогут перенести заявку.")]
    Private,
}

