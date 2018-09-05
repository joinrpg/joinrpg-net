using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using System.Reflection;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JoinRpg.Web.Models
{
    public enum ClaimDenialStatusView
    {
        [Display(Name = "Мастера не могут предоставить желаемую роль"), UsedImplicitly]
        Unavailable,
        [Display(Name = "Игрок сам отказался, но не отозвал заявку"), UsedImplicitly]
        Refused,
        [Display(Name = "Игрок не отвечает"), UsedImplicitly]
        NotRespond,
        [Display(Name = "Мастера снимают игрока с роли"), UsedImplicitly]
        Removed,
        [Display(Name = "Мастера игроку на игре не рады"), UsedImplicitly]
        NotSuitable,
        [Display(Name = "Игрок не выполнил условия участия или не сдал взнос"), UsedImplicitly]
        NotImplementable,
    }

    public static class ClaimDenialStatusViewExtensions
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .First()
                            .GetCustomAttribute<DisplayAttribute>()
                            .GetName();
        }
    }

}
