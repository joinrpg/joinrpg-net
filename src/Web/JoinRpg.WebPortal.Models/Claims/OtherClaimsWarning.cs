using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.Web.Models;

/// <summary>
/// Перечень заявок для предупреждения на странице заявки (#6089): не больше
/// <see cref="MaxDisplayedClaims"/> ссылок, остальные перечислены числом. Рисуется компонентом
/// <c>OtherClaimsWarningView</c>.
/// </summary>
public record class OtherClaimsWarning(IReadOnlyList<ClaimLinkViewModel> DisplayedClaims, int HiddenClaimsCount)
{
    public const int MaxDisplayedClaims = 10;

    public static OtherClaimsWarning Empty { get; } = new([], 0);

    public bool HasClaims => DisplayedClaims.Count > 0 || HiddenClaimsCount > 0;

    /// <summary>
    /// Предупреждение при принятии заявки: другие активные заявки игрока в проекте, которые будут
    /// автоматически отклонены. Бессмысленно, где конкурентов нет: у утверждённой заявки они уже
    /// отклонены, а где разрешено несколько персонажей — не отклонятся.
    /// </summary>
    public static OtherClaimsWarning ForApproveClaim(IReadOnlyCollection<ClaimWithPlayer> activeClaimHeaders,
        ClaimIdentification currentClaimId, bool acceptingDeclinesOtherClaims)
        => acceptingDeclinesOtherClaims
            ? CreateLinks(activeClaimHeaders, currentClaimId, _ => true)
            : Empty;

    /// <summary>
    /// Предупреждение при восстановлении отклонённой заявки: утверждённые заявки игрока в проекте,
    /// которые блокируют восстановление. Блокировка возможна только где разрешён один персонаж
    /// (<paramref name="strictlyOneCharacter"/>) — то же правило, что у
    /// <see cref="ClaimValidator.CanRestoreClaim"/>: «утверждена» — это
    /// <see cref="UserClaimInfo.IsApproved"/> (Approved или CheckedIn) из доменного снимка игрока.
    /// </summary>
    public static OtherClaimsWarning ForRestoreClaim(IReadOnlyCollection<ClaimWithPlayer> activeClaimHeaders,
        ClaimIdentification currentClaimId, IReadOnlyCollection<UserClaimInfo> playerActiveClaims,
        bool strictlyOneCharacter)
    {
        if (!strictlyOneCharacter)
        {
            return Empty;
        }
        var approvedClaimIds = playerActiveClaims.Where(claim => claim.IsApproved).Select(claim => claim.ClaimId).ToHashSet();
        return CreateLinks(activeClaimHeaders, currentClaimId, header => approvedClaimIds.Contains(header.ClaimId));
    }

    public static OtherClaimsWarning Create(IEnumerable<ClaimLinkViewModel> claims)
    {
        var allClaims = claims.ToArray();
        return new([.. allClaims.Take(MaxDisplayedClaims)], Math.Max(0, allClaims.Length - MaxDisplayedClaims));
    }

    /// <summary>Мапит заголовки в ссылки, отбрасывая текущую заявку; порядок — по возрастанию id (раньше поданные первыми).</summary>
    private static OtherClaimsWarning CreateLinks(IEnumerable<ClaimWithPlayer> headers,
        ClaimIdentification currentClaimId, Func<ClaimWithPlayer, bool> extraFilter)
        => Create(headers
            .Where(header => header.ClaimId != currentClaimId && extraFilter(header))
            .OrderBy(header => header.ClaimId.ClaimId)
            .Select(ToLink));

    private static ClaimLinkViewModel ToLink(ClaimWithPlayer header)
        => new(header.ClaimId, header.Player.DisplayName, header.CharacterName, header.ExtraNicknames ?? "", header.Player.UserId);
}
