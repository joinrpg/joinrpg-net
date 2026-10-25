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

    /// <summary>
    /// Регистрацией в обход не обойти: заявка должна быть одобрена и ещё не зарегистрирована.
    /// </summary>
    public bool CanCheckInWithOverrideInPrinciple => NotCheckedInAlready && IsApproved;

    /// <summary>
    /// Что придётся подтвердить, чтобы зарегистрировать игрока в обход. <c>null</c> — если обходить
    /// нечего (регистрация и так возможна) или обойти нельзя (заявка не одобрена или уже зарегистрирована).
    /// </summary>
    public CheckInOverride? RequiredOverride
        => !CanCheckInWithOverrideInPrinciple || CanCheckInNow
            ? null
            : new([.. Problems.Select(CheckInBlockerKey.From)], Math.Max(FeeDue, 0));

    /// <summary>
    /// Можно ли зарегистрировать игрока в обход с этим подтверждением: всё, что держит регистрацию
    /// сейчас, мастер подтвердил.
    /// </summary>
    public bool CanCheckInWithOverride(CheckInOverride checkInOverride)
        => CanCheckInWithOverrideInPrinciple
           && Problems.All(problem => checkInOverride.Problems.Contains(CheckInBlockerKey.From(problem)))
           && FeeDue <= checkInOverride.FeeDue;
}
