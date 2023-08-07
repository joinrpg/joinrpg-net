using JetBrains.Annotations;

namespace JoinRpg.Domain.Problems;

public enum ClaimProblemType
{
    NoResponsibleMaster,
    InvalidResponsibleMaster,
    ClaimNeverAnswered,
    ClaimNoDecision,
    ClaimActiveButCharacterHasApprovedClaim,
    FinanceModerationRequired,
    TooManyMoney,
    ClaimDiscussionStopped,
    NoCharacterOnApprovedClaim,
    FeePaidPartially,
    UnApprovedClaimPayment,
    ClaimWorkStopped,
    ClaimDontHaveTarget,
    [Obsolete, UsedImplicitly]
    DeletedFieldHasValue,
    FieldIsEmpty,
    FieldShouldNotHaveValue,
    NoParentGroup,
    GroupIsBroken,
}
