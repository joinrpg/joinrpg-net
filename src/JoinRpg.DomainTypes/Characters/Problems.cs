using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DomainTypes.Characters;

[method: JsonConstructor]
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
    /// <summary>
    /// Заявка не привязана к персонажу. Недостижимо: в доменном агрегате (ADR013) заявка
    /// существует только как элемент <c>CharacterInfo.Claims</c>, и <c>ClaimProblemContext</c>
    /// проверяет это в конструкторе. Значение остаётся в enum, потому что числовые значения
    /// уезжают в сериализованные вью-модели островов, а отображение — чтобы уже сохранённые
    /// где-то проблемы не ломали страницу.
    /// </summary>
    [Obsolete("Недостижимо по построению агрегата персонажа, см. BrokenClaimsAndCharacters")]
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

public record class FieldRelatedProblem(ClaimProblemType ProblemType, ProblemSeverity Severity, ProjectFieldInfo Field, string? ExtraInfo = null)
    : ClaimProblem(ProblemType, Severity, Field.Name + ExtraInfo ?? "")
{
    public ProjectFieldInfo Field { get; } = Field ?? throw new ArgumentNullException(nameof(Field));
}

public record class ProfileRelatedProblem(ClaimProblemType ProblemType, ProblemSeverity ProblemSeverity) : ClaimProblem(ProblemType, ProblemSeverity);
