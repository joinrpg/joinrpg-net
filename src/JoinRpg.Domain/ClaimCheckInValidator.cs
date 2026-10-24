using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain;

/// <summary>
/// Можно ли зарегистрировать игрока по этой заявке. Считается целиком по доменным сущностям
/// (ADR013, #4892): EF-графа здесь больше нет.
/// </summary>
public class ClaimCheckInValidator(ClaimInfo context, IClaimProblemValidator claimValidator)
{
    private readonly CharacterClaimInfo claim = context.Claim;

    /// <summary>
    /// Сколько осталось доплатить. Было <c>claim.ClaimFeeDue(projectInfo)</c> — та же величина
    /// «начислено минус уплачено», но посчитанная по EF-графу.
    /// </summary>
    public int FeeDue => context.ClaimInCharacter.CalculateBalance().FeeDue;

    public bool NotCheckedInAlready => claim.CheckInDate == null &&
                                       claim.Status != ClaimStatus.CheckedIn;

    public bool IsApproved => claim.Status == ClaimStatus.Approved;

    /// <summary>
    /// Что не даёт зарегистрировать игрока: незаполненные поля заявки и данные профиля, которые
    /// требует проект (#5708).
    /// </summary>
    public IReadOnlyCollection<ClaimProblem> Problems { get; } = [.. claimValidator.ValidateCheckInBlockers(context)];

    public bool CanCheckInInPrinciple => NotCheckedInAlready && IsApproved && Problems.Count == 0;

    public bool CanCheckInNow => CanCheckInInPrinciple && FeeDue <= 0;

    // TODO: временный хак — на проекте 1612 взнос при регистрации не отмечается. Взнос и долг
    // заявки при этом не меняются, хак действует только на форму регистрации и её проверку в сервисе.
    private bool SkipFeeOnCheckIn => context.ProjectInfo.ProjectId.Value == 1612;

    /// <summary>
    /// Сколько принять на стойке регистрации. Обычно это <see cref="FeeDue"/>.
    /// </summary>
    public int FeeToAcceptOnCheckIn => SkipFeeOnCheckIn ? 0 : FeeDue;

    /// <summary>
    /// Можно ли зарегистрировать игрока прямо сейчас — проверка самой операции регистрации.
    /// </summary>
    public bool CanCheckInAtDesk => CanCheckInInPrinciple && FeeToAcceptOnCheckIn <= 0;
}
