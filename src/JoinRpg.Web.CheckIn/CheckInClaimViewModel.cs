using System.ComponentModel.DataAnnotations;
using JoinRpg.Common.WebComponents;
using JoinRpg.Web.Claims;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.Web.CheckIn;

/// <summary>
/// Форма регистрации игрока на стойке. Правила регистрации уже посчитаны (ClaimCheckInValidator),
/// здесь только их итог.
/// </summary>
public class CheckInClaimViewModel
{
    public required int ProjectId { get; init; }
    public required int ClaimId { get; init; }

    [Display(Name = "Ответственный мастер")]
    public required UserLinkViewModel Master { get; init; }
    public required UserProfileDetailsViewModel PlayerDetails { get; init; }
    public required ClaimStatusView ClaimStatus { get; init; }

    [UIHint("EventTime")]
    public required DateTime? CheckInTime { get; init; }

    public required bool CheckedInAlready { get; init; }
    public required bool IsApproved { get; init; }

    /// <summary>
    /// Зарегистрировать можно, как только будет принят взнос <see cref="FeeDue"/>.
    /// </summary>
    public required bool CanCheckInInPrinciple { get; init; }

    /// <summary>
    /// Сколько принять на стойке.
    /// </summary>
    public required int FeeDue { get; init; }

    /// <summary>
    /// Может ли этот мастер принять взнос наличными.
    /// </summary>
    public required bool CanAcceptFee { get; init; }

    /// <summary>
    /// Что не даёт зарегистрировать игрока. Сами паспортные данные стойке не показываются — только
    /// факт, что их не хватает.
    /// </summary>
    public required IReadOnlyCollection<CheckInProblemViewModel> Problems { get; init; }

    public required string CurrentUserFullName { get; init; }
}
