using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace JoinRpg.CommonUI.Models
{
  public enum CommentExtraAction
  {
    [Display(Name = "Финансовая операция подтверждена", ShortName = "отмечено")]
    [UsedImplicitly]
    ApproveFinance,
    [Display(Name = "Финансовая операция отклонена", ShortName = "отмечено")]
    [UsedImplicitly]
    RejectFinance,
    [Display(Name = "Заявка одобрена мастером", ShortName = "одобрена")]
    [UsedImplicitly]
    ApproveByMaster,
    [Display(Name = "Заявка отклонена мастером", ShortName = "отклонена")]
    [UsedImplicitly]
    DeclineByMaster,
    [Display(Name = "Заявка восстановлена мастером", ShortName = "восстановлена")]
    [UsedImplicitly]
    RestoreByMaster,
    [Display(Name = "Заявка перемещена мастером", ShortName = "изменена")]
    [UsedImplicitly]
    MoveByMaster,
    [Display(Name = "Заявка отозвана игроком", ShortName = "отозвана")]
    [UsedImplicitly]
    DeclineByPlayer,
    [Display(Name = "Ответственный мастер изменен", ShortName = "изменена")]
    [UsedImplicitly]
    ChangeResponsible,
    [Display(Name = "Новая заявка", ShortName = "подана")]
    [UsedImplicitly]
    NewClaim,
    [Display(Name = "Заявка поставлена в лист ожидания", ShortName = "изменена")]
    [UsedImplicitly]
    OnHoldByMaster,
    [Display(Name = "Сумма взноса установлена вручную", ShortName = "изменена")]
    [UsedImplicitly]
    FeeChanged,
    [Display(Name = "Пройдена регистрация на полигоне", ShortName = "изменена")]
    [UsedImplicitly]
    CheckedIn,
  }
}