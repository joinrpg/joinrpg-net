using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models.CharacterGroups;

public enum DirectClaimSettings
{
    [Display(Name = "Заявки вне прописанных мастерами персонажей запрещены")]
    NoDirectClaims,
    [Display(Name = "Разрешены заявки в группу (без лимита)")]
    DirectClaimsUnlimited,
    [Display(Name = "Разрешены заявки в группу, но не более лимита")]
    DirectClaimsLimited,
}
