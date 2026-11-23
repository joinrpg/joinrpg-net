using System.Globalization;
using System.Net;

namespace JoinRpg.Services.Advertisement.Sending;

internal class TelegramNewlyOpenedProjectsDigestSender(
    TelegramChatId chatId,
    ITelegramNotificationService telegramNotificationService,
    IUriLocator<ProjectIdentification> projectUriLocator) : INewlyOpenedProjectsDigestSender
{
    public Task<SendingResult> Send(IReadOnlyList<(ProjectAdvertisementCandidate Project, KogdaIgraGameData Game)> entries)
    {
        var message = BuildMessage(entries, p => projectUriLocator.GetUri(p.ProjectId));
        return telegramNotificationService.SendTelegramNotification(chatId, message);
    }

    // TODO[Localize]
    internal static TelegramHtmlString BuildMessage(
        IReadOnlyList<(ProjectAdvertisementCandidate Project, KogdaIgraGameData Game)> entries,
        Func<ProjectAdvertisementCandidate, Uri> getProjectUri)
    {
        var lines = entries.Select(e => BuildLine(e.Project, e.Game, getProjectUri(e.Project)));

        var text =
            "<strong>На прошлой неделе открылись заявки:</strong>\n\n" +
            string.Join("\n", lines) + "\n\n" +
            "<em>Сообщение от joinrpg</em>";

        return new TelegramHtmlString(text);
    }

    private static string BuildLine(ProjectAdvertisementCandidate project, KogdaIgraGameData game, Uri projectUri)
    {
        // Как и в TelegramSingleHotRoleSender, только <strong>/<em>/<a> переживают санитайзер Telegram.
        var masterGroupSuffix = string.IsNullOrWhiteSpace(game.MasterGroupName)
            ? string.Empty
            : $" ({WebUtility.HtmlEncode($"«{game.MasterGroupName}»")})";

        var dateRangeString = DateRangeFormatter.FormatDisplay(
            new DateRange(game.Begin, game.End), CultureInfo.GetCultureInfo("ru-RU"));

        return $"«<a href=\"{projectUri}\">{WebUtility.HtmlEncode(project.ProjectName.Value)}</a>», " +
            $"{WebUtility.HtmlEncode(game.RegionName)}{masterGroupSuffix}, {dateRangeString} ";
    }
}
