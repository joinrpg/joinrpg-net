using System.Diagnostics.Metrics;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Notifications;

[JobDelay(7)] // UTC 7 часов = Москва 10 утра
internal class DailyChangedPlayerClaimsNotificationJob(
    IClaimsRepository claimsRepository,
    ILogger<DailyChangedPlayerClaimsNotificationJob> logger,
    IUserRepository userRepository,
    ITelegramNotificationService telegramNotificationService,
    IUriService uriService
    ) : IDailyJob
{
    private static readonly Meter meter = new("JoinRpg");
    private static readonly Counter<int> playersWithChangedClaimsHistogram = meter.CreateCounter<int>("joinrpg.notification.players_with_changed_claims");
    private static readonly Counter<int> playersWithChangedClaimsAndTelegramHistogram = meter.CreateCounter<int>("joinrpg.notification.players_with_changed_claims_with_telegram");
    private static readonly Counter<int> playersWithChangedClaimsAndTelegramEnabledHistogram = meter.CreateCounter<int>("joinrpg.notification.players_with_changed_claims_with_telegram_enabled");

    private int playersWithChangedClaimsAndTelegram, playersWithChangedClaimsAndTelegramEnabled, playersCount;

    public async Task RunOnce(CancellationToken cancellationToken)
    {
        var sinceTime = DateTime.UtcNow.AddHours(-24).AddMinutes(-20);
        // Интервалы будут частично перекрываться, чтобы это предотвратить, нужно записывать какие конкретно изменения были отправлены
        var updates = await claimsRepository.GetUpdatedClaimsSince(sinceTime);
        logger.LogInformation("Найдено {updatedClaimsCount} обновлений {since}", updates.Count, sinceTime);

        foreach (var userUpdates in updates.GroupBy(u => u.UserId))
        {
            await SendNotificationForPlayer(userUpdates);
        }
        playersWithChangedClaimsHistogram.Add(playersCount);
        playersWithChangedClaimsAndTelegramHistogram.Add(playersWithChangedClaimsAndTelegram);
        playersWithChangedClaimsAndTelegramEnabledHistogram.Add(playersWithChangedClaimsAndTelegramEnabled);
        logger.LogInformation("Итого {playersCount} обновлений, {playersWithChangedClaimsAndTelegram} c телеграммом, {playersWithChangedClaimsAndTelegramEnabled} с телеграммом и включенными уведомлениями",
            playersCount, playersWithChangedClaimsAndTelegram, playersWithChangedClaimsAndTelegramEnabled);
    }

    private async Task SendNotificationForPlayer(IGrouping<PrimitiveTypes.UserIdentification, UpdatedClaimDto> userUpdates)
    {
        var user = await userRepository.GetUserInfo(userUpdates.Key);
        var userUpdatesList = userUpdates.ToList();
        if (user is null)
        {
            logger.LogError("Не нашли пользователя {userId}", userUpdates.Key);
            return;
        }
        logger.LogInformation("У пользователя {userId} {userName} было {userUpdatedClaimsCount} обновлений в заявках. Телеграм подключен: {telegramConnected}. Обновления включены: {telegramEnabled}",
            user.UserId, user.DisplayName, userUpdatesList.Count, user.Social.TelegramId is not null, user.NotificationSettings.TelegramDigestEnabled);

        playersCount++;

        if (user.Social.TelegramId is not null)
        {
            playersWithChangedClaimsAndTelegram++;
            if (user.NotificationSettings.TelegramDigestEnabled)
            {
                playersWithChangedClaimsAndTelegramEnabled++;
            }
            else
            {
                return;
            }
        }
        else
        {
            return;
        }

        var message = $@"Привет, {user.DisplayName.DisplayName}!
Твои заявки на игры были обновлены за последний день:
{string.Join("\n", userUpdatesList.Select(update => $"<a href=\"{GetClaimUri(update).AbsoluteUri}\">{update.CharacterName}</a>  на игру {update.ProjectName.Value}"))}
";

        // TODO Если ты не хочешь получать это сообщение, отключи его в настройках;

        await telegramNotificationService.SendTelegramNotification(user.Social.TelegramId, new TelegramHtmlString(message));
    }

    private Uri GetClaimUri(UpdatedClaimDto update) => uriService.GetUri(update.ClaimId);
}
