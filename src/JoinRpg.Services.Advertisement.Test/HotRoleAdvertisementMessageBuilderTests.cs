namespace JoinRpg.Services.Advertisement.Test;

public class HotRoleAdvertisementMessageBuilderTests
{
    [Fact]
    public Task BuildMessage_WhenMasterGroupIsEmpty_DoesNotShowEmptyBrackets()
    {
        var uri = new Uri("https://joinrpg.ru/character/1/claim");

        var kogdaIgraGame = new KogdaIgraGameData(
            new KogdaIgraIdentification(1),
            "Зимний путь 2026",
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 5),
            "Московская область",
            "",
            SiteUri: null,
            IsActive: true);

        var kogdaIgraCardUri = new Uri("https://kogda-igra.ru/game/1/");

        var message = TelegramSingleHotRoleSender.BuildMessage(
            "Тёмный властелин",
            "Зимний путь",
            new MarkdownString("Описание персонажа"),
            kogdaIgraGame,
            kogdaIgraCardUri,
            uri);

        return Verify(message.Contents);
    }

    [Fact]
    public Task BuildMessage_IncludesAllSuppliedData()
    {
        var uri = new Uri("https://joinrpg.ru/character/1/claim");

        var kogdaIgraGame = new KogdaIgraGameData(
            new KogdaIgraIdentification(1),
            "Зимний путь 2026",
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 5),
            "Московская область",
            "Мастерская группа «Северный ветер»",
            SiteUri: null,
            IsActive: true);

        var kogdaIgraCardUri = new Uri("https://kogda-igra.ru/game/1/");

        var message = TelegramSingleHotRoleSender.BuildMessage(
            "Тёмный властелин",
            "Зимний путь",
            new MarkdownString("Описание персонажа"),
            kogdaIgraGame,
            kogdaIgraCardUri,
            uri);

        return Verify(message.Contents);
    }
}
