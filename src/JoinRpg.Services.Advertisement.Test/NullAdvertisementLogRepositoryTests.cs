using JoinRpg.DataModel;
using JoinRpg.Services.Advertisement.Log;

namespace JoinRpg.Services.Advertisement.Test;

public class NullAdvertisementLogRepositoryTests
{
    [Fact]
    public async Task GetHotCharactersAdvertisementInfo_ReturnsOnlyCharactersOfRequestedProjectWithZeroStats()
    {
        var projectOne = new ProjectIdentification(1);
        var projectTwo = new ProjectIdentification(2);
        var characterInProjectOne = MakeCharacter(projectOne, 1);
        var characterInProjectTwo = MakeCharacter(projectTwo, 2);
        var hotCharactersRepository = new FakeHotCharactersRepository([characterInProjectOne, characterInProjectTwo]);
        var repository = new NullAdvertisementLogRepository(hotCharactersRepository);

        var result = await repository.GetHotCharactersAdvertisementInfo(new AdvertisementScheduleIdentification(1), projectOne);

        var info = result.ShouldHaveSingleItem();
        info.Character.ShouldBe(characterInProjectOne);
        info.AdvertisementCount.ShouldBe(0);
        info.AlreadySentForSchedule.ShouldBeFalse();
    }

    [Fact]
    public async Task RecordAdvertisement_DoesNotThrow()
    {
        var repository = new NullAdvertisementLogRepository(new FakeHotCharactersRepository([]));
        var entry = new AdvertisementLogEntryInfo(
            new AdvertisementScheduleIdentification(1),
            AdvertisementMethod.SingleHotRole,
            new ProjectIdentification(1),
            new CharacterIdentification(new ProjectIdentification(1), 1),
            AdvertisementLogStatus.Sent,
            DateTimeOffset.UtcNow);

        await repository.RecordAdvertisement(entry);
    }

    private static CharacterWithProject MakeCharacter(ProjectIdentification projectId, int characterId) =>
        new(
            new CharacterIdentification(projectId, characterId),
            CharacterName: $"Character {characterId}",
            IsPublic: true,
            IsActive: true,
            new ProjectName("Test project"),
            new MarkdownDbValue(),
            new MarkdownDbValue(),
            []);

    private sealed class FakeHotCharactersRepository(IReadOnlyCollection<CharacterWithProject> characters) : IHotCharactersRepository
    {
        public Task<IReadOnlyCollection<CharacterWithProject>> GetHotCharactersFromPublicProjects(KeySetPagination? pagination = null) =>
            Task.FromResult(characters);
    }
}
