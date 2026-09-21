namespace JoinRpg.Services.Advertisement.Test;

public class NewlyOpenedProjectsDigestMessageBuilderTests
{
    private static Uri GetProjectUri(ProjectAdvertisementCandidate p) => new($"https://joinrpg.ru/{p.ProjectId.Value}/home");

    private static ProjectAdvertisementCandidate MakeCandidate(int projectId, string projectName) =>
        new(new ProjectIdentification(projectId), new ProjectName(projectName), ActiveClaimsCount: 3, AdvertisementCount: 0);

    private static KogdaIgraGameData MakeGame(string regionName, string masterGroupName, DateOnly begin, DateOnly end) =>
        new(new KogdaIgraIdentification(1), "Игра КогдаИгра", begin, end, regionName, masterGroupName, SiteUri: null, IsActive: true);

    [Fact]
    public Task BuildMessage_SingleProject_IncludesNameLinkAndGameDetails()
    {
        var entries = new[]
        {
            (MakeCandidate(1, "Зимний путь"), MakeGame("Московская область", "Северный ветер", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5))),
        };

        var message = TelegramNewlyOpenedProjectsDigestSender.BuildMessage(entries, GetProjectUri);

        return Verify(message.Contents);
    }

    [Fact]
    public Task BuildMessage_MultipleProjects_ListsAll()
    {
        var entries = new[]
        {
            (MakeCandidate(1, "Зимний путь"), MakeGame("Московская область", "Северный ветер", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5))),
            (MakeCandidate(2, "Летний ветер"), MakeGame("Лениградская область", "Южный Юг", new DateOnly(2026, 7, 10), new DateOnly(2026, 7, 12))),
        };

        var message = TelegramNewlyOpenedProjectsDigestSender.BuildMessage(entries, GetProjectUri);

        return Verify(message.Contents);
    }

    [Fact]
    public Task BuildMessage_WhenMasterGroupIsEmpty_DoesNotShowEmptyBrackets()
    {
        var entries = new[]
        {
            (MakeCandidate(1, "Зимний путь"), MakeGame("Московская область", "", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5))),
        };

        var message = TelegramNewlyOpenedProjectsDigestSender.BuildMessage(entries, GetProjectUri);

        return Verify(message.Contents);
    }

    [Fact]
    public Task BuildMessage_ProjectNameWithHtmlSpecialChars_EscapesThem()
    {
        var entries = new[]
        {
            (MakeCandidate(1, "<Игра> & \"Компания\""), MakeGame("Московская область", "", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5))),
        };

        var message = TelegramNewlyOpenedProjectsDigestSender.BuildMessage(entries, GetProjectUri);

        return Verify(message.Contents);
    }

    [Theory]
    [InlineData(1, "игру")]
    [InlineData(2, "игры")]
    [InlineData(4, "игры")]
    [InlineData(5, "игр")]
    [InlineData(11, "игр")]
    [InlineData(14, "игр")]
    [InlineData(21, "игру")]
    [InlineData(25, "игр")]
    public void BuildMessage_HeaderUsesCorrectPluralForm(int count, string expectedWord)
    {
        var entries = Enumerable.Range(1, count)
            .Select(i => (MakeCandidate(i, $"Игра {i}"), MakeGame("Московская область", "", new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 5))))
            .ToList();

        var message = TelegramNewlyOpenedProjectsDigestSender.BuildMessage(entries, GetProjectUri);

        message.Contents.ShouldContain($"открылись заявки на {expectedWord}:");
    }
}
