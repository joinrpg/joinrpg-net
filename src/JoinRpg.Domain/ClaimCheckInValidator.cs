using JoinRpg.Domain.Problems;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain;

/// <summary>
/// Можно ли зарегистрировать игрока по этой заявке. Считается целиком по доменным сущностям
/// (ADR013, #4892): EF-графа здесь больше нет.
/// </summary>
public class ClaimCheckInValidator(ClaimProblemContext context, IClaimProblemValidator claimValidator)
{
    private readonly CharacterClaimInfo claim = context.Claim;

    /// <summary>
    /// Сколько осталось доплатить. Было <c>claim.ClaimFeeDue(projectInfo)</c> — та же величина
    /// «начислено минус уплачено», но посчитанная по EF-графу.
    /// </summary>
    public int FeeDue => context.Character.CalculateClaimBalance(claim, context.ProjectInfo).FeeDue;

    public bool NotCheckedInAlready => claim.CheckInDate == null &&
                                       claim.Status != ClaimStatus.CheckedIn;

    public bool IsApproved => claim.Status == ClaimStatus.Approved;

    public IReadOnlyCollection<FieldRelatedProblem> FieldProblems { get; } = [.. claimValidator.ValidateFieldsOnly(context)];

    public bool CanCheckInInPrinciple => NotCheckedInAlready && IsApproved && FieldProblems.Count == 0;

    public bool CanCheckInNow => CanCheckInInPrinciple && FeeDue <= 0;
}
