namespace JoinRpg.Services.Advertisement.Test;

public class AdvertisementGameRankingTests
{
    [Fact]
    public void OrderByPriority_NeverAdvertisedProject_WinsOverAdvertisedProject()
    {
        var neverAdvertised = MakeCandidate(1, activeClaimsCount: 10, advertisementCount: 0);
        var advertisedALot = MakeCandidate(2, activeClaimsCount: 10, advertisementCount: 5);

        var result = AdvertisementGameRanking.OrderByPriority([advertisedALot, neverAdvertised]).ToList();

        result.ShouldBe([neverAdvertised, advertisedALot]);
    }

    [Fact]
    public void OrderByPriority_WhenBothAdvertised_OrdersByActiveClaimsPerAdvertisement()
    {
        var lowRatio = MakeCandidate(1, activeClaimsCount: 2, advertisementCount: 1);
        var highRatio = MakeCandidate(2, activeClaimsCount: 10, advertisementCount: 1);

        var result = AdvertisementGameRanking.OrderByPriority([lowRatio, highRatio]).ToList();

        result.ShouldBe([highRatio, lowRatio]);
    }

    private static ProjectAdvertisementCandidate MakeCandidate(int projectId, int activeClaimsCount, int advertisementCount) =>
        new(new ProjectIdentification(projectId), activeClaimsCount, advertisementCount);
}
