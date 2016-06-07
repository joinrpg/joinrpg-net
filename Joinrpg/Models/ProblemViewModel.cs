using System;
using System.Collections.Generic;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models
{
  public class ProblemViewModel
  {
    //Mass usage of Display(Name) turns out too slow...
    private static readonly IDictionary<ClaimProblemType, string> Types = new Dictionary<ClaimProblemType, string>()
    {
      {ClaimProblemType.NoResponsibleMaster, "Не назначен мастер"},
      {ClaimProblemType.InvalidResponsibleMaster, "Неверный мастер"},
      {ClaimProblemType.ClaimNeverAnswered, "Заявка без ответа"},
      {ClaimProblemType.ClaimNoDecision, "По заявке нет решения"},
      {ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim, "Персонаж уже занят"},
      {ClaimProblemType.FinanceModerationRequired, "Взнос на модерации"},
      {ClaimProblemType.TooManyMoney, "Есть переплата по взносу"},
      {ClaimProblemType.ClaimDiscussionStopped, "Обсуждение остановилось"},
      {ClaimProblemType.NoCharacterOnApprovedClaim, "Нет персонажа у заявки"},
      {ClaimProblemType.FeePaidPartially, "Взнос уплачен частично"},
      {ClaimProblemType.UnApprovedClaimPayment, "Оплата в непринятой заявке"},
      {ClaimProblemType.ClaimWorkStopped, "Работа по заявке остановлена"},
      {ClaimProblemType.ClaimDontHaveTarget, "Заявка не привязана ни к чему"},
      {ClaimProblemType.DeletedFieldHasValue, "Значение в удаленном поле"},
      {ClaimProblemType.FieldIsEmpty, "Поле не заполнено"},
      {ClaimProblemType.FieldShouldNotHaveValue, "Поле не должно быть заполнено для этой группы"}
    };

    public ProblemViewModel(ClaimProblem problem)
    {
      ProblemType = Types[problem.ProblemType];
      ProblemTime = problem.ProblemTime;
      Severity = problem.Severity;
      Extra = problem.ExtraInfo;
    }

    public string ProblemType { get; }

    public DateTime? ProblemTime { get; }

    public ProblemSeverity Severity { get; }

    public string Extra { get; }
  }

}