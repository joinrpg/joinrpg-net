using JoinRpg.Web.CheckIn;
using JoinRpg.Web.Models.Characters;
using JoinRpg.Web.Models.Print;

namespace JoinRpg.Portal.Models;

/// <summary>
/// Страница регистрации игрока: форма регистрации и то, что вокруг неё на странице персонажа.
/// </summary>
public record class CheckInClaimPageModel(
    CheckInClaimViewModel CheckIn,
    CharacterNavigationViewModel Navigation,
    IReadOnlyCollection<HandoutListItemViewModel> Handouts);
