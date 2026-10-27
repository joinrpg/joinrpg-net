using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Проблемы заявки, посчитанные по доменным сущностям (ADR013).
/// </summary>
/// <remarks>
/// Зеркало <see cref="ICharacterProblemValidator"/> для стороны заявки. <see cref="ProjectInfo"/>
/// параметром не передаётся — он приходит внутри <see cref="ClaimInfo"/>.
/// </remarks>
public interface IClaimProblemValidator
{
    IEnumerable<ClaimProblem> Validate(ClaimInfo context, ProblemSeverity minimalSeverity = ProblemSeverity.Hint);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context);

    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(ClaimInfo context, IEnumerable<ProjectFieldIdentification> fields);

    IEnumerable<FieldRelatedProblem> ValidateFieldOnly(ClaimInfo context, ProjectFieldIdentification fieldId)
        => ValidateFieldsOnly(context, [fieldId]);

    /// <summary>
    /// Проблемы, которые не дают зарегистрировать игрока на полигоне (<see cref="ClaimProblem.BlocksCheckIn"/>).
    /// </summary>
    IEnumerable<ClaimProblem> ValidateCheckInBlockers(ClaimInfo context)
        => Validate(context).Where(problem => problem.BlocksCheckIn);

    /// <summary>
    /// Что игроку нужно заполнить самому: поля заявки, которые он может редактировать, и данные
    /// профиля, которые требует проект. То же, что держит регистрацию, за вычетом мастерского.
    /// </summary>
    IEnumerable<ClaimProblem> ValidatePlayerToFill(ClaimInfo context)
        => ValidateCheckInBlockers(context).Where(problem => problem.PlayerCanFix);

    /// <summary>
    /// Проблемы заявки, которые можно показать самому игроку.
    /// </summary>
    /// <remarks>
    /// Берутся те, что видны игроку при текущем статусе заявки (<see cref="ClaimProblem.VisibleToPlayer"/>).
    /// Проблемы полей и взноса дополнительно ограничены настройкой проекта
    /// <see cref="ProjectClaimSettings.ShowCheckInBlockersToPlayers"/>: без неё игрок их не видит вовсе,
    /// а проблемы профиля показываются независимо от неё.
    /// </remarks>
    PlayerClaimProblems ValidateForPlayer(ClaimInfo context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var claimStatus = context.Claim.Status;
        var showCheckInBlockers = context.ProjectInfo.ClaimSettings.ShowCheckInBlockersToPlayers;

        var problems = Validate(context).ToList();

        var visible = problems
            .Where(problem => problem.VisibleToPlayer(claimStatus))
            .Where(problem => showCheckInBlockers || problem is ProfileRelatedProblem)
            .ToList();

        var hasHiddenCheckInBlockers = showCheckInBlockers
            && claimStatus == ClaimStatus.Approved
            && problems.Any(problem => problem is FieldRelatedProblem { BlocksCheckIn: true } && !problem.VisibleToPlayer(claimStatus));

        return new PlayerClaimProblems(visible, hasHiddenCheckInBlockers);
    }
}

/// <summary>
/// Проблемы заявки глазами игрока (см. <see cref="IClaimProblemValidator.ValidateForPlayer"/>).
/// </summary>
/// <param name="Problems">Проблемы, которые можно показать игроку.</param>
/// <param name="HasHiddenCheckInBlockers">
/// Регистрацию держат проблемы полей, которых игрок не видит: ему можно сказать, что они есть,
/// но не какие. Считается, только если проект показывает игрокам проблемы полей
/// (<see cref="ProjectClaimSettings.ShowCheckInBlockersToPlayers"/>) и регистрация заявке ещё предстоит
/// (принята, но не зарегистрирована): на непринятой мастерские поля держат её лишь формально,
/// у зарегистрированной держать уже нечего, и совет обратиться к мастеру ввёл бы игрока
/// в заблуждение. Иначе всегда <c>false</c>.
/// </param>
public record PlayerClaimProblems(IReadOnlyCollection<ClaimProblem> Problems, bool HasHiddenCheckInBlockers);
