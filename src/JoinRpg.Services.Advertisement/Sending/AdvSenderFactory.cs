using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Advertisement.Sending;

internal class AdvSenderFactory(
    ITelegramNotificationService telegramNotificationService,
    IOptions<KogdaIgraOptions> kogdaIgraOptions,
    IUriLocator<ProjectIdentification> projectUriLocator) : IAdvSenderFactory
{
    public ISingleHotRoleSender? Create(AdvertisementChannelInfo channel)
    {
        if (TryGetTelegramChatId(channel) is not { } chatId)
        {
            return null;
        }

        return new TelegramSingleHotRoleSender(chatId, telegramNotificationService, kogdaIgraOptions.Value);
    }

    public INewlyOpenedProjectsDigestSender? CreateNewlyOpenedProjectsDigestSender(AdvertisementChannelInfo channel)
    {
        if (TryGetTelegramChatId(channel) is not { } chatId)
        {
            return null;
        }

        return new TelegramNewlyOpenedProjectsDigestSender(chatId, telegramNotificationService, projectUriLocator);
    }

    private static TelegramChatId? TryGetTelegramChatId(AdvertisementChannelInfo channel)
    {
        if (channel.Settings is not TelegramChannelSettings telegramSettings)
        {
            return null;
        }

        if (telegramSettings.ChatId.Id == 0)
        {
            return null;
        }

        return telegramSettings.ChatId;
    }
}
