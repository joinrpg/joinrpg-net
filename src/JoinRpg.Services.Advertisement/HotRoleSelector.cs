namespace JoinRpg.Services.Advertisement;

internal static class HotRoleSelector
{
    public static CharacterAdvertisementInfo? SelectLeastAdvertised(
        IReadOnlyCollection<CharacterAdvertisementInfo> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var minCount = candidates.Min(c => c.AdvertisementCount);
        return candidates
            .Where(c => c.AdvertisementCount == minCount)
            .OrderByDescending(c => c.Character.CharacterId.CharacterId)
            .First();
    }
}
