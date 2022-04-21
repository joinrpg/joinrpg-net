using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public enum ContactsAccessTypeView
{
    [Display(Name = "Только мастерам",
        Description = "Ссылки на ваши соцсети будут доступны только мастерам игры. Другие игроки не смогут увидеть о вас ничего, кроме предпочитаемого имени и аватарки.")]
    OnlyForMasters = 0,
    [Display(Name = "Всем",
        Description = "Ссылки на ваши соцсети будут доступны всем, что позволит другим игрокам находить вас в соцсетях. Это не покажет им вашу почту, телефон или настоящее имя, если они не публичны в ваших соцсетях.")]
    Public = 1,
}
