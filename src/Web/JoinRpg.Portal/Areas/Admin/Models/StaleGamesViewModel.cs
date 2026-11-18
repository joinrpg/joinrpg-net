using JoinRpg.Data.Interfaces;

namespace JoinRpg.Portal.Areas.Admin.Models;

public record StaleGamesViewModel(
    IReadOnlyCollection<ProjectWithUpdateDateDto> Stale,
    IReadOnlyCollection<ProjectWithUpdateDateDto> Blocked);
