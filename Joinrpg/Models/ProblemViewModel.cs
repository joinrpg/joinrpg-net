using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
  public class ProblemViewModel
  {
    public ProblemViewModel(ClaimProblem problem)
    {
      ProblemType = (ProblemTypeViewModel) problem.ProblemType;
      ProblemTime = problem.ProblemTime;
      Severity = problem.Severity;
      Extra = problem.ExtraInfo;
    }

    public ProblemTypeViewModel ProblemType { get;  }

    public DateTime? ProblemTime { get; }

    public ProblemSeverity Severity { get;  }

    public string Extra { get; }
  }


  /// <summary>
  /// <see cref="ClaimProblemType"/>
  /// </summary>
  public enum ProblemTypeViewModel
  {
    [Display(Name = "Не назначен мастер")]
    [UsedImplicitly]
    NoResponsibleMaster,
    [Display(Name = "Неверный мастер")]
    [UsedImplicitly]
    InvalidResponsibleMaster,
    [Display(Name = "Заявка без ответа")]
    [UsedImplicitly]
    ClaimNeverAnswered,
    [Display(Name = "По заявке нет решения")]
    [UsedImplicitly]
    ClaimNoDecision,
    [UsedImplicitly]
    [Display(Name = "Персонаж уже занят")]
    ClaimActiveButCharacterHasApprovedClaim,
    [UsedImplicitly]
    [Display(Name = "Взнос на модерации")]
    FinanceModerationRequired,
    [UsedImplicitly]
    [Display(Name = "Есть переплата по взносу")]
    TooManyMoney,
    [UsedImplicitly]
    [Display(Name = "Обсуждение остановилось")]
    ClaimDiscussionStopped,
    [UsedImplicitly]
    [Display(Name = "Нет персонажа у заявки")]
    NoCharacterOnApprovedClaim,
    [UsedImplicitly]
    [Display(Name = "Взнос уплачен частично")]
    FeePaidPartially,
    [UsedImplicitly]
    [Display(Name = "Оплата в непринятой заявке")]
    UnApprovedClaimPayment,
    [UsedImplicitly]
    [Display(Name = "Работа по заявке остановлена")]
    ClaimWorkStopped,
    [UsedImplicitly]
    [Display(Name = "Заявка не привязана ни к чему")]
    ClaimDontHaveTarget,
    [UsedImplicitly]
    [Display(Name = "Значение в удаленном поле")]
    DeletedFieldHasValue,
    [UsedImplicitly]
    [Display(Name = "Поле не заполнено")]
    FieldIsEmpty,
    [UsedImplicitly]
    [Display(Name="Поле не должно быть заполнено для этой группы")]
    FieldShouldNotHaveValue
  }
}