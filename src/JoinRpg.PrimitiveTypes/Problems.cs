namespace JoinRpg.PrimitiveTypes;
public record class ClaimProblem(ClaimProblemType ProblemType, DateTime? ProblemTime, string? ExtraInfo, ProblemSeverity Severity)
{
    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, DateTime problemTime, string? extraInfo = null) : this(problemType, problemTime, extraInfo, severity)
    {
    }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, string extraInfo) : this(problemType, null, extraInfo, severity)
    {
    }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity) : this(problemType, null, null, severity)
    {
    }

    public ClaimProblem(ClaimProblemType problemType, ProblemSeverity severity, DateTimeOffset problemTime, string? extraInfo = null) : this(problemType, problemTime.DateTime, extraInfo, severity)
    {
    }
}

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
    [Obsolete]
    DeletedFieldHasValue,
    FieldIsEmpty,
    FieldShouldNotHaveValue,
    NoParentGroup,
    GroupIsBroken,
    InActiveVariant,
    MissingTelegram,
    MissingVkontakte,
    MissingPhone,
    MissingRealname,
    MissingPassport,
    MissingRegistrationAddress,
    SensitiveDataNotAllowed,
}

public enum ProblemSeverity
{
    Hint,
    Warning,
    Error,
    Fatal,
}
