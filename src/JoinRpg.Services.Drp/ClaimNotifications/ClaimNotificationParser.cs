using System.Globalization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Helpers;
using UiCommentExtraAction = JoinRpg.CommonUI.Models.CommentExtraAction;

namespace JoinRpg.Services.Drp.ClaimNotifications;

/// <summary>
/// Разбирает уведомления по заявкам (формат <c>ClaimNotificationTextBuilder</c>) в события.
/// Используется для восстановления заявок после потери основной БД (DRP, #5481).
/// </summary>
/// <remarks>
/// Формат строки уведомления по заявке:
/// <code>
/// Header: {Проект}: {Персонаж}, игрок {Игрок}
/// Body:   Добрый день, {получатель}
///         Заявка {Персонаж} игрока {Игрок} {действие} {мастером X | игроком}
///
///         [**{название действия}**
///
///         ][Сумма операции: N
///
///         ]{текст}
///         [
///         Страница заявки: {url}]   — до 2026-06-07
/// </code>
/// Строки разных получателей одного события отличаются только приветствием, а время создания у них
/// общее (<c>NotificationServiceImpl.SaveToQueue</c>) — по этому их и склеиваем.
/// </remarks>
public static class ClaimNotificationParser
{
    private const string GreetingPrefix = "Добрый день, ";
    private const string ClaimLinePrefix = "Заявка ";
    private const string PlayerInClaimLine = " игрока ";
    private const string PlayerInHeader = ", игрок ";
    private const string ByPlayer = "игроком";
    private const string ByMaster = "мастером ";
    private const string CommentShortName = "откомментирована";
    private const string MoneyPrefix = "Сумма операции: ";
    private const string LegacyClaimLinkPrefix = "Страница заявки: ";
    /// <summary>Заголовок приглашений к проживанию (<c>AccommodationNotificationTextBuilder</c>).</summary>
    private const string AccommodationInviteHeaderSuffix = ": приглашения к проживанию";

    private static readonly Dictionary<string, CommentExtraAction> ActionsByDisplayName =
        Enum.GetValues<UiCommentExtraAction>().ToDictionary(a => a.GetDisplayName(), a => (CommentExtraAction)a);

    /// <summary>
    /// Склеивает строки получателей в события и разбирает их. Уведомления не по заявкам пропускает.
    /// Порядок — по времени события.
    /// </summary>
    public static IReadOnlyList<ClaimNotificationEvent> Parse(IEnumerable<NotificationMessageForRecipient> notifications)
    {
        return [.. notifications
            .Select(n => (Notification: n, ClaimId: TryGetClaimId(n.EntityReference), Body: SplitGreeting(n.Body.Value)))
            .Where(x => x.ClaimId is not null)
            .GroupBy(x => (x.Notification.EntityReference, x.Notification.CreatedAt, x.Notification.Initiator, x.Notification.Header, x.Body.Text))
            .Select(g => ParseEvent(
                g.First().ClaimId!,
                g.Key.EntityReference!,
                g.Key.CreatedAt,
                g.Key.Initiator,
                g.Key.Header,
                g.Key.Text,
                [.. g.Select(x => new ClaimNotificationRecipient(x.Notification.Recipient, x.Body.GreetingName)).Distinct()]))
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.CommentId)];
    }

    internal static ClaimIdentification? TryGetClaimId(IProjectEntityId? reference) => reference switch
    {
        ClaimIdentification claim => claim,
        ClaimCommentIdentification comment => comment.ClaimId,
        FinanceOperationIdentification operation => operation.ClaimId,
        _ => null,
    };

    /// <summary>Отделяет приветствие (строку 1) — только им строки разных получателей и отличаются.</summary>
    private static (string? GreetingName, string Text) SplitGreeting(string body)
    {
        var normalized = body.Replace("\r\n", "\n");
        if (!normalized.StartsWith(GreetingPrefix, StringComparison.Ordinal))
        {
            return (null, normalized);
        }
        var lineEnd = normalized.IndexOf('\n');
        var greetingLine = lineEnd < 0 ? normalized : normalized[..lineEnd];
        var rest = lineEnd < 0 ? "" : normalized[(lineEnd + 1)..];
        // В уведомлениях по заявке после имени ничего нет, а само имя — свободный текст («Петров А.»),
        // поэтому знаки препинания не срезаем: имя должно совпасть с именем игрока в заголовке.
        var name = greetingLine[GreetingPrefix.Length..].Trim();
        return (name.Length == 0 ? null : name, rest);
    }

    private static ClaimNotificationEvent ParseEvent(
        ClaimIdentification claimId,
        IProjectEntityId reference,
        DateTimeOffset createdAt,
        UserIdentification initiator,
        string header,
        string rest,
        IReadOnlyList<ClaimNotificationRecipient> recipients)
    {
        var lineEnd = rest.IndexOf('\n');
        var claimLine = lineEnd < 0 ? rest : rest[..lineEnd];
        var afterClaimLine = lineEnd < 0 ? "" : rest[(lineEnd + 1)..];

        if (TryParseClaimLine(header, claimLine) is { } line)
        {
            var (action, money, text) = line.IsComment
                ? (null, null, afterClaimLine)
                : ParseActionBlock(afterClaimLine);

            if (line.IsComment || action is not null)
            {
                return new ClaimNotificationEvent(claimId, reference, createdAt, initiator, ClaimNotificationKind.ClaimChange,
                    line.CharacterName, line.PlayerName, action, line.Author, line.MasterName, money, CleanText(text), recipients);
            }
        }

        var kind = reference switch
        {
            FinanceOperationIdentification => ClaimNotificationKind.OnlinePayment,
            _ when header.EndsWith(AccommodationInviteHeaderSuffix, StringComparison.Ordinal) => ClaimNotificationKind.AccommodationInvite,
            _ => ClaimNotificationKind.Other,
        };
        return new ClaimNotificationEvent(claimId, reference, createdAt, initiator, kind,
            CharacterName: null, PlayerName: null, Action: null, Author: null, MasterName: null, Money: null, CleanText(rest), recipients);
    }

    private sealed record ClaimLine(string CharacterName, string PlayerName, bool IsComment, ClaimEventAuthor Author, string? MasterName);

    /// <summary>
    /// «Заявка {Персонаж} игрока {Игрок} {действие} {кем}» сверяется с заголовком «{Проект}: {Персонаж}, игрок {Игрок}»:
    /// разделители могут встречаться и внутри имён, а пара строк вместе однозначна почти всегда.
    /// </summary>
    private static ClaimLine? TryParseClaimLine(string header, string claimLine)
    {
        if (!claimLine.StartsWith(ClaimLinePrefix, StringComparison.Ordinal))
        {
            return null;
        }
        var line = claimLine[ClaimLinePrefix.Length..];

        for (var playerAt = line.IndexOf(PlayerInClaimLine, StringComparison.Ordinal);
             playerAt >= 0;
             playerAt = line.IndexOf(PlayerInClaimLine, playerAt + 1, StringComparison.Ordinal))
        {
            var characterName = line[..playerAt];
            var afterPlayer = line[(playerAt + PlayerInClaimLine.Length)..];

            // Заголовок кончается на «{Персонаж}, игрок {Игрок}» — отсюда имя игрока.
            var characterInHeader = ": " + characterName + PlayerInHeader;
            var headerAt = header.IndexOf(characterInHeader, StringComparison.Ordinal);
            if (headerAt < 0)
            {
                continue;
            }
            var playerName = header[(headerAt + characterInHeader.Length)..];
            if (!afterPlayer.StartsWith(playerName + " ", StringComparison.Ordinal))
            {
                continue;
            }

            var actionAndAuthor = afterPlayer[(playerName.Length + 1)..];
            if (TryParseActionAndAuthor(actionAndAuthor) is { } parsed)
            {
                return new ClaimLine(characterName, playerName, parsed.ShortName == CommentShortName, parsed.Author, parsed.MasterName);
            }
        }
        return null;
    }

    /// <summary>
    /// «{действие} мастером {имя}» или «{действие} игроком». Мастер — первым: в коротких названиях действий
    /// «мастером» не встречается, а имя мастера может кончаться на «игроком».
    /// </summary>
    private static (string ShortName, ClaimEventAuthor Author, string? MasterName)? TryParseActionAndAuthor(string actionAndAuthor)
    {
        var masterAt = actionAndAuthor.IndexOf(" " + ByMaster, StringComparison.Ordinal);
        if (masterAt > 0)
        {
            return (actionAndAuthor[..masterAt], ClaimEventAuthor.Master, actionAndAuthor[(masterAt + ByMaster.Length + 1)..]);
        }
        if (actionAndAuthor.EndsWith(" " + ByPlayer, StringComparison.Ordinal))
        {
            return (actionAndAuthor[..^(ByPlayer.Length + 1)], ClaimEventAuthor.Player, null);
        }
        return null;
    }

    /// <summary>
    /// «**{название действия}**», затем необязательная «Сумма операции: N», затем текст.
    /// Неизвестное название — null: событие не распознано.
    /// </summary>
    private static (CommentExtraAction? Action, int? Money, string Text) ParseActionBlock(string block)
    {
        var rest = block.TrimStart('\n');
        if (!rest.StartsWith("**", StringComparison.Ordinal))
        {
            return (null, null, block);
        }
        var nameEnd = rest.IndexOf("**\n", 2, StringComparison.Ordinal);
        if (nameEnd < 0 || !ActionsByDisplayName.TryGetValue(rest[2..nameEnd], out var action))
        {
            return (null, null, block);
        }
        rest = rest[(nameEnd + 3)..].TrimStart('\n');

        int? money = null;
        if (rest.StartsWith(MoneyPrefix, StringComparison.Ordinal))
        {
            var moneyEnd = rest.IndexOf('\n');
            var moneyText = moneyEnd < 0 ? rest[MoneyPrefix.Length..] : rest[MoneyPrefix.Length..moneyEnd];
            if (int.TryParse(moneyText.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                money = value;
                rest = moneyEnd < 0 ? "" : rest[(moneyEnd + 1)..];
            }
        }
        return (action, money, rest);
    }

    /// <summary>
    /// Убирает пустые строки по краям и строку со ссылкой на заявку, которую писали в тело до 2026-06-07.
    /// </summary>
    private static string CleanText(string text)
    {
        var trimmed = text.Trim('\n');
        var lastLineAt = trimmed.LastIndexOf('\n');
        var lastLine = lastLineAt < 0 ? trimmed : trimmed[(lastLineAt + 1)..];
        if (lastLine.StartsWith(LegacyClaimLinkPrefix, StringComparison.Ordinal))
        {
            trimmed = lastLineAt < 0 ? "" : trimmed[..lastLineAt].TrimEnd('\n');
        }
        return trimmed;
    }
}
