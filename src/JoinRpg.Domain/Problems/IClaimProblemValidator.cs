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
}
