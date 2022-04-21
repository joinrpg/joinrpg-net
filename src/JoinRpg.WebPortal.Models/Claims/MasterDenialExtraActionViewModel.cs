using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.Models;

public enum MasterDenialExtraActionViewModel
{
    [Display(Name = "...сохранить", Description = "Заявка будет отклонена, но персонаж останется в сетке ролей и другие игроки смогут на него заявиться.")]
    KeepCharacter,
    [Display(Name = "...удалить", Description = "Заявка будет отклонена и персонаж удален и недоступен для заявок. В случае необходимости его можно будет восстановить.")]
    DeleteCharacter,
}

