using JoinRpg.DataModel;

namespace JoinRpg.Services.Advertisement.Test;

public class HotRoleSelectorTests
{
    [Fact]
    public void SelectLeastAdvertised_EmptyCandidates_ReturnsNull()
    {
        var result = HotRoleSelector.SelectLeastAdvertised([]);

        result.ShouldBeNull();
    }

    [Fact]
    public void SelectLeastAdvertised_PicksRoleWithMinimalAdvertisementCount()
    {
        var leastAdvertised = MakeInfo(1, advertisementCount: 0);
        var mostAdvertised = MakeInfo(2, advertisementCount: 5);

        var result = HotRoleSelector.SelectLeastAdvertised([mostAdvertised, leastAdvertised]);

        result.ShouldBe(leastAdvertised);
    }

    [Fact]
    public void SelectLeastAdvertised_WhenTied_PicksMostRecentlyCreatedCandidate()
    {
        var older = MakeInfo(1, advertisementCount: 0);
        var newer = MakeInfo(2, advertisementCount: 0);
        var notTied = MakeInfo(3, advertisementCount: 3);

        var result = HotRoleSelector.SelectLeastAdvertised([older, notTied, newer]);

        result.ShouldBe(newer);
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
