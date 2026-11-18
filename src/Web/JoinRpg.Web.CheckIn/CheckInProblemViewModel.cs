using System.ComponentModel.DataAnnotations;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.CheckIn;

/// <summary>
/// Проблема, которая держит регистрацию: незаполненное поле заявки или данные профиля.
/// </summary>
public record class CheckInProblemViewModel(ClaimProblem Problem) : ProblemViewModel(Problem)
{
    public WhoWllFillEnum WhoWillFill { get; } = Problem.PlayerCanFix ? WhoWllFillEnum.Player : WhoWllFillEnum.Master;

    /// <summary>
    /// Что именно не так: название поля или «В профиле нет паспортных данных».
    /// </summary>
    public string Title => Problem is FieldRelatedProblem fieldProblem ? fieldProblem.Field.Name : ProblemTypeText;

    /// <summary>
    /// Уточнение для поля («Поле не заполнено»); у проблем профиля всё сказано в <see cref="Title"/>.
    /// </summary>
    public string? Details => Problem is FieldRelatedProblem ? ProblemTypeText : null;

    public enum WhoWllFillEnum
    {
        [Display(Name = "Мастер")]
        Master,
        [Display(Name = "Игрок")]
        Player,
    }
}
