namespace JoinRpg.Services.Advertisement;

internal static class AdvertisementGameRanking
{
    // +1 в знаменателе, чтобы никогда не рекламированные проекты (AdvertisementCount == 0)
    // естественно получали максимальный вес без отдельной ветки для деления на ноль.
    public static IEnumerable<ProjectAdvertisementCandidate> OrderByPriority(
        IEnumerable<ProjectAdvertisementCandidate> candidates) =>
        candidates.OrderByDescending(c => (c.ActiveClaimsCount + 1.0) / (c.AdvertisementCount + 1));
}
