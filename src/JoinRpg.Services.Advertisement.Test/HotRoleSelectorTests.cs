using JoinRpg.DataModel;
using JoinRpg.Services.Advertisement.Log;

namespace JoinRpg.Services.Advertisement.Test;

public class HotRoleSelectorTests
{
    [Fact]
    public void SelectLeastAdvertised_EmptyCandidates_ReturnsNull()
    {
        var result = HotRoleSelector.SelectLeastAdvertised([], new Random(1));

        result.ShouldBeNull();
    }

    [Fact]
    public void SelectLeastAdvertised_PicksRoleWithMinimalAdvertisementCount()
    {
        var leastAdvertised = MakeInfo(1, advertisementCount: 0);
        var mostAdvertised = MakeInfo(2, advertisementCount: 5);

        var result = HotRoleSelector.SelectLeastAdvertised([mostAdvertised, leastAdvertised], new Random(1));

        result.ShouldBe(leastAdvertised);
    }

    [Fact]
    public void SelectLeastAdvertised_WhenTied_PicksFromTiedCandidatesOnly()
    {
        var tiedA = MakeInfo(1, advertisementCount: 0);
        var tiedB = MakeInfo(2, advertisementCount: 0);
        var notTied = MakeInfo(3, advertisementCount: 3);

        for (var seed = 0; seed < 20; seed++)
        {
            var result = HotRoleSelector.SelectLeastAdvertised([tiedA, tiedB, notTied], new Random(seed));
            result.ShouldBeOneOf(tiedA, tiedB);
        }
    }

    private static CharacterAdvertisementInfo MakeInfo(int characterId, int advertisementCount) =>
        new(
            new CharacterWithProject(
                new CharacterIdentification(new ProjectIdentification(1), characterId),
                CharacterName: $"Character {characterId}",
                IsPublic: true,
                IsActive: true,
                new ProjectName("Test project"),
                new MarkdownDbValue(),
                new MarkdownDbValue(),
                []),
            advertisementCount,
            AlreadySentForSchedule: false);
}
