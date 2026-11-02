using System.Text.Json.Serialization;
using JoinRpg.DomainTypes.Characters.Claims;
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

    /// <summary>
    /// Не даёт зарегистрировать игрока на полигоне (#5708): пока проблема есть, заявка не готова к регистрации.
    /// </summary>
    [JsonIgnore]
    public virtual bool BlocksCheckIn => false;

    /// <summary>
    /// Исправить может сам игрок; иначе — только мастер.
    /// </summary>
    [JsonIgnore]
    public virtual bool PlayerCanFix => false;

    /// <summary>
    /// Проблему можно показать игроку, подавшему заявку. По умолчанию — нельзя: работа мастеров
    /// с заявкой игрока не касается, и новый тип проблемы не должен утечь к нему, пока о нём не
    /// решили явно. Исключение — деньги самого игрока и объяснение, почему его заявка не
    /// принята автоматически.
    /// </summary>
    /// <param name="claimStatus">
    /// Статус заявки: от него зависит, какие поля игрок видит (см. <see cref="FieldRelatedProblem"/>).
    /// Для остальных проблем не важен.
    /// </param>
    public virtual bool VisibleToPlayer(ClaimStatus claimStatus) => ProblemType switch
    {
        ClaimProblemType.FeePaidPartially => true,
        ClaimProblemType.UnApprovedClaimPayment => true,
        ClaimProblemType.CheckedInNotPaid => true,
        ClaimProblemType.SlotExhausted => true,
        _ => false,
    };

    /// <summary>
    /// Проблема мешает заявке быть принятой автоматически: заявка-кандидат
    /// (<see cref="Claims.ClaimInfo.IsAutoAcceptCandidate"/>) с такой проблемой принята
    /// автоматически не будет. В проекте с включённым автоприёмом эти проблемы показывают
    /// игроку — они объясняют, почему его заявка не принята сама.
    /// </summary>
    [JsonIgnore]
    public virtual bool IsAutoAcceptBlocker => ProblemType switch
    {
        ClaimProblemType.SlotExhausted => true,
        // Не выданный игроком доступ к защищённым данным — тоже блокер автоприёма; игроку эта
        // проблема и так видна всегда, см. ProfileRelatedProblem.
        ClaimProblemType.SensitiveDataNotAllowed => true,
        _ => false,
    };

    /// <summary>
    /// Показывается игроку, даже если проект не показывает игрокам проблемы регистрации
    /// (<c>ProjectCheckInSettings.ShowCheckInBlockersToPlayers</c>): это не работа мастеров,
    /// о которой игроку знать не положено, а то, что игрок может понять и сам — например,
    /// его профиль.
    /// </summary>
    [JsonIgnore]
    public virtual bool AlwaysVisibleToPlayer => false;
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
    /// существует только как элемент <c>CharacterInfo.Claims</c>, и <c>ClaimInCharacter</c>
    /// проверяет это в конструкторе. Значение остаётся в enum, потому что числовые значения
    /// уезжают в сериализованные вью-модели островов и сдвиг их поломал бы. Отображения у него
    /// при этом нет: тест-страж <c>ProblemTypeDisplayTest</c> требует, чтобы у Obsolete-значения
    /// отображения не было, — так же устроен соседний <c>DeletedFieldHasValue</c>.
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
    MissingBirthDate,
    /// <summary>
    /// Заявка зарегистрирована на полигоне, а взнос уплачен не полностью.
    /// </summary>
    CheckedInNotPaid,
    /// <summary>
    /// Заявка висит на шаблоне, в котором не осталось свободных мест: принять её нельзя,
    /// пока мастер не поднимет лимит (#4743). При включённом автоприёме — ещё и причина,
    /// почему заявка не принята автоматически.
    /// </summary>
    SlotExhausted,
}

public enum ProblemSeverity
{
    Hint,
    Warning,
    Error,
    Fatal,
}

public record class FieldRelatedProblem(ClaimProblemType ProblemType, ProblemSeverity Severity, ProjectFieldInfo Field, string? ExtraInfo = null)
    : ClaimProblem(ProblemType, Severity, Field.Name + ExtraInfo)
{
    public ProjectFieldInfo Field { get; } = Field ?? throw new ArgumentNullException(nameof(Field));

    /// <summary>
    /// Незаполненное поле — любой важности — держит регистрацию: так было и до #5708.
    /// </summary>
    public override bool BlocksCheckIn => true;

    public override bool PlayerCanFix => Field.CanPlayerEdit;

    /// <summary>
    /// Только если игрок видит само поле на заявке в этом статусе: в тексте проблемы его название,
    /// а название скрытого от игрока поля показывать нельзя.
    /// </summary>
    public override bool VisibleToPlayer(ClaimStatus claimStatus) => Field.IsVisibleToClaimPlayer(claimStatus);
}

public record class ProfileRelatedProblem(ClaimProblemType ProblemType, ProblemSeverity ProblemSeverity) : ClaimProblem(ProblemType, ProblemSeverity)
{
    /// <summary>
    /// Держит то, что проект требует (Warning и выше), подсказки по рекомендованным контактам — нет.
    /// Запрошенные паспорт, адрес регистрации и дата рождения — тоже Warning (см. <c>UserProfileProblemsCalculator.ToSensitiveDataSeverity</c>).
    /// </summary>
    public override bool BlocksCheckIn => Severity >= ProblemSeverity.Warning;

    /// <summary>
    /// Профиль и согласие на доступ к нему — только сам игрок.
    /// </summary>
    public override bool PlayerCanFix => true;

    /// <summary>
    /// Это профиль самого игрока — ему и показываем.
    /// </summary>
    public override bool VisibleToPlayer(ClaimStatus claimStatus) => true;

    /// <summary>
    /// Профиль игрока — не работа мастеров, прятать его от игрока не нужно.
    /// </summary>
    public override bool AlwaysVisibleToPlayer => true;
}
