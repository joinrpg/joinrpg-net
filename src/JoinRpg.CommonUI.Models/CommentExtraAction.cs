using System.ComponentModel.DataAnnotations;

namespace JoinRpg.CommonUI.Models;

public enum CommentExtraAction
{
    [Display(Name = "Финансовая операция подтверждена", ShortName = "отмечено")]
    ApproveFinance,

    [Display(Name = "Финансовая операция отклонена", ShortName = "отмечено")]
    RejectFinance,

    [Display(Name = "Заявка одобрена мастером", ShortName = "одобрена")]
    ApproveByMaster,

    [Display(Name = "Заявка отклонена мастером", ShortName = "отклонена")]
    DeclineByMaster,

    [Display(Name = "Заявка восстановлена мастером", ShortName = "восстановлена")]
    RestoreByMaster,

    [Display(Name = "Заявка перемещена мастером", ShortName = "изменена")]
    MoveByMaster,

    [Display(Name = "Заявка отозвана игроком", ShortName = "отозвана")]
    DeclineByPlayer,

    [Display(Name = "Ответственный мастер изменен", ShortName = "изменена")]
    ChangeResponsible,

    [Display(Name = "Новая заявка", ShortName = "подана")]
    NewClaim,

    [Display(Name = "Заявка поставлена в лист ожидания", ShortName = "изменена")]
    OnHoldByMaster,

    [Display(Name = "Сумма взноса установлена вручную", ShortName = "изменена")]
    FeeChanged,

    [Display(Name = "Пройдена регистрация на полигоне", ShortName = "изменена")]
    CheckedIn,

    [Display(Name = "Выпущен второй ролью", ShortName = "изменена")]
    SecondRole,

    [Display(Name = "Игрок вышел из игры", ShortName = "изменена")]
    OutOfGame,

    [Display(Name = "Запрос льготного взноса", ShortName = "изменена")]
    RequestPreferential,

    [Display(Name = "Оплачен взнос", ShortName = "отмечено")]
    PaidFee,

    [Display(Name = "Оплачен взнос", ShortName = "отмечено")]
    PcsbOnlineFeeAccepted,
}
