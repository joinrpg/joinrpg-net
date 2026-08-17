using System.Net;
using JoinRpg.Markdown;

namespace JoinRpg.Services.Advertisement.Sending;

internal class TelegramSingleHotRoleSender(
    TelegramId chatId,
    ITelegramNotificationService telegramNotificationService,
    KogdaIgraOptions kogdaIgraOptions) : ISingleHotRoleSender
{
    public Task<SendingResult> Send(ProjectInfo project, DomainTypes.ProjectMetadata.ProjectDetails details, Character character, Uri addClaimUri)
    {
        var nearestFutureCard = details.NearestFutureKogdaIgraCard
            ?? throw new InvalidOperationException($"У проекта {project.ProjectId} нет карточки КогдаИгры в будущем");

        var kogdaIgraCardUri = new Uri(kogdaIgraOptions.HostName, $"/game/{nearestFutureCard.Id.Value}/");

        var message = BuildMessage(
            character.CharacterName,
            project.ProjectName,
            character.Description,
            nearestFutureCard,
            kogdaIgraCardUri,
            addClaimUri);

        return telegramNotificationService.SendTelegramNotification(chatId, message);
    }

    // TODO[Localize]
    internal static TelegramHtmlString BuildMessage(
        string characterName,
        string projectName,
        MarkdownString? characterDescription,
        KogdaIgraGameData kogdaIgraGame,
        Uri kogdaIgraCardUri,
        Uri addClaimUri)
    {
        // Только <strong>/<em>/<a> переживают санитайзер Telegram (см. HtmlSanitizers.InitTelegramSanitizer) —
        // заголовки (<h1>-<h6>) и <small> им не поддерживаются и вырезаются целиком вместе с содержимым,
        // поэтому вместо них используем <strong>/<em> и обычные переносы строк.
        var text =
            $"<strong>Горячая роль «{WebUtility.HtmlEncode(characterName)}» на игре «{WebUtility.HtmlEncode(projectName)}»</strong>\n\n" +
            $"{WebUtility.HtmlEncode(kogdaIgraGame.Name)}, {WebUtility.HtmlEncode(kogdaIgraGame.RegionName)} ({WebUtility.HtmlEncode(kogdaIgraGame.MasterGroupName)})\n" +
            $"{kogdaIgraGame.Begin:dd.MM.yyyy}–{kogdaIgraGame.End:dd.MM.yyyy}\n" +
            $"{kogdaIgraCardUri}\n\n" +
            $"<strong>{WebUtility.HtmlEncode(characterName)}</strong>\n\n" +
            $"{characterDescription.ToHtmlString().Value}\n\n" +
            $"<a href=\"{addClaimUri}\">Подать заявку</a>\n\n" +
            "<em>Сообщение от joinrpg</em>";
        return new TelegramHtmlString(text);
    }
}
