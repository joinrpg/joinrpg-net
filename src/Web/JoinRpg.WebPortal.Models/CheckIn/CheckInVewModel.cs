using JoinRpg.Data.Interfaces;

namespace JoinRpg.Web.Models.CheckIn;

public class CheckInIndexViewModel(ProjectInfo project, CheckedInBannerViewModel? justCheckedIn = null) : IProjectIdAware
{
    public ProjectIdentification ProjectId { get; } = project.ProjectId;

    public ClaimIdentification? ClaimId { get; set; }

    /// <summary>Кого только что зарегистрировали — для баннера с печатью бейджа. <c>null</c> — баннера нет</summary>
    public CheckedInBannerViewModel? JustCheckedIn { get; } = justCheckedIn;

    int IProjectIdAware.ProjectId => ProjectId;
}

/// <param name="CanPrintBadge">Есть ли в проекте шаблоны бейджей — иначе кнопку печати не показываем</param>
public record CheckedInBannerViewModel(string PlayerName, string CharacterName, CharacterIdentification CharacterId, bool CanPrintBadge);

public class CheckInListItemViewModel(ClaimWithPlayer claim)
{
    public string OtherNicks { get; } = claim.ExtraNicknames ?? "";

    public string NickName { get; } = claim.Player.DisplayName.DisplayName;

    public string Fullname { get; } = claim.Player.DisplayName.FullName ?? "";

    public string CharacterName { get; } = claim.CharacterName;

    public int ClaimId { get; } = claim.ClaimId.ClaimId;
}
