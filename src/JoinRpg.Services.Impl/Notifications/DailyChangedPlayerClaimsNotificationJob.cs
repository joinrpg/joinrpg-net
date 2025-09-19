using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl.Notifications;
internal class DailyChangedPlayerClaimsNotificationJob(
    IClaimsRepository claimsRepository,
    ILogger<DailyChangedPlayerClaimsNotificationJob> logger,
    IUserRepository userRepository,
    ITelegramNotificationService telegramNotificationService,
    IUriService uriService
    ) : IDailyJob
{
    public async Task RunOnce(CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromHours(8), cancellationToken); //UTC 8 утра = Москва 11 утра
        var sinceTime = DateTime.UtcNow.AddHours(-25);
        // Интервалы будут частично перекрываться, чтобы это предотвратить, нужно записывать какие конкретно изменения были отправлены
        var updates = await claimsRepository.GetUpdatedClaimsSince(sinceTime);
        logger.LogInformation("Найдено {updatedClaimsCount} обновлений {since}", updates.Count, sinceTime);
        foreach (var userUpdates in updates.GroupBy(u => u.UserId))
        {
            var user = await userRepository.GetUserInfo(userUpdates.Key);
            var userUpdatesList = userUpdates.ToList();
            if (user is null)
            {
                logger.LogError("Не нашли пользователя {userId}", userUpdates.Key);
                continue;
            }
            logger.LogInformation("У пользователя {userId} {userName} было {userUpdatedClaimsCount} обновлений в заявках. Телеграм подключен: {telegramConnected}. Обновления включены: {telegramEnabled}",
                user.UserIdentification, user.Name, userUpdatesList.Count, user.Social.TelegramId is not null, user.NotificationSettings.TelegramDigestEnabled);

            if (user.Social.TelegramId is null || !user.NotificationSettings.TelegramDigestEnabled)
            {
                return;
            }

            var message = $@"Привет, {user.Name.DisplayName}!
Твои заявки на игры были обновлены за последний день:
{string.Join("\n", userUpdatesList.Select(update => $"<a href=\"{GetClaimUri(update).AbsoluteUri}\">{update.CharacterName}</a>  на игру {update.ProjectName.Value}"))}
";

            // TODO Если ты не хочешь получать это сообщение, отключи его в настройках;

            await telegramNotificationService.SendTelegramNotification(user.Social.TelegramId, new TelegramHtmlString(message));

        }
    }

    private Uri GetClaimUri(UpdatedClaimDto update) => uriService.GetUri(update.ClaimId);
}
