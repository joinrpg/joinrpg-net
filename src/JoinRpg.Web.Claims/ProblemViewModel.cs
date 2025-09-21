using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Claims;

public class ProblemViewModel(ClaimProblem problem)
{
    //Mass usage of Display(Name) turns out too slow...
    private static readonly Dictionary<ClaimProblemType, string> Types = new()
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
      {ClaimProblemType.FieldIsEmpty, "Поле не заполнено"},
      {ClaimProblemType.FieldShouldNotHaveValue, "Поле не должно быть заполнено для этой группы"},
      {ClaimProblemType.NoParentGroup, "Персонаж не принадлежит ни к одной группе"},
      {ClaimProblemType.GroupIsBroken, "Группа сломана"},
      {ClaimProblemType.InActiveVariant, "Выбран удаленный вариант значения" },
      {ClaimProblemType.MissingRealname, "В профиле нет ФИО" },
      {ClaimProblemType.MissingPhone, "В профиле нет телефона" },
      {ClaimProblemType.MissingTelegram, "Не привязан телеграмм" },
      {ClaimProblemType.MissingVkontakte, "Не привязан ВК" },
      {ClaimProblemType.MissingPassport, "В профиле нет паспортных данных" },
      {ClaimProblemType.MissingRegistrationAddress, "В профиле нет адреса регистрации" },
      {ClaimProblemType.SensitiveDataNotAllowed, "Не разрешен доступ к паспортным данным" },
    };

    public string ProblemTypeText { get; } = Types[problem.ProblemType];

    public ClaimProblemType ProblemType { get; } = problem.ProblemType;

    public DateTime? ProblemTime { get; } = problem.ProblemTime;

    public ProblemSeverity Severity { get; } = problem.Severity;

    public string? Extra { get; } = problem.ExtraInfo;

    public bool ProfileRelated { get; } = problem is ProfileRelatedProblem;
}

