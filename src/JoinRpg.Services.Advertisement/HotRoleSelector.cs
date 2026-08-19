namespace JoinRpg.Services.Advertisement;

internal static class HotRoleSelector
{
    public static CharacterAdvertisementInfo? SelectLeastAdvertised(
        IReadOnlyCollection<CharacterAdvertisementInfo> candidates, Random random)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var minCount = candidates.Min(c => c.AdvertisementCount);
        var leastAdvertised = candidates.Where(c => c.AdvertisementCount == minCount).ToList();
        return leastAdvertised[random.Next(leastAdvertised.Count)];
    }
}
