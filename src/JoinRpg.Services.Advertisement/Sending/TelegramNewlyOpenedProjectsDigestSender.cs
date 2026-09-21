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
            $"<strong>На прошлой неделе открылись заявки на {GetGameWord(entries.Count)}:</strong>\n\n" +
            string.Join("\n", lines) + "\n\n" +
            "<em>Сообщение от joinrpg</em>";

        return new TelegramHtmlString(text);
    }

    // Винительный падеж: "заявки на игру/игры/игр" — не переиспользуем нигде больше, поэтому не выносим в общий хелпер.
    private static string GetGameWord(int count)
    {
        var mod100 = count % 100;
        if (mod100 is >= 11 and <= 14)
        {
            return "игр";
        }

        return (count % 10) switch
        {
            1 => "игру",
            >= 2 and <= 4 => "игры",
            _ => "игр",
        };
    }

    private static string BuildLine(ProjectAdvertisementCandidate project, KogdaIgraGameData game, Uri projectUri)
    {
        // Как и в TelegramSingleHotRoleSender, только <strong>/<em>/<a> переживают санитайзер Telegram.
        var masterGroupSuffix = string.IsNullOrWhiteSpace(game.MasterGroupName)
            ? string.Empty
            : $" ({WebUtility.HtmlEncode($"Мастерская группа «{game.MasterGroupName}»")})";

        var dateRangeString = DateRangeFormatter.FormatDisplay(
            new DateRange(game.Begin, game.End), CultureInfo.GetCultureInfo("ru-RU"));

        return $"«<a href=\"{projectUri}\">{WebUtility.HtmlEncode(project.ProjectName.Value)}</a>», " +
            $"{WebUtility.HtmlEncode(game.RegionName)}{masterGroupSuffix}, {dateRangeString} ";
    }
}
