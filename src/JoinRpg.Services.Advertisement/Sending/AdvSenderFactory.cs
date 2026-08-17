using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Advertisement.Sending;

internal class AdvSenderFactory(ITelegramNotificationService telegramNotificationService, IOptions<KogdaIgraOptions> kogdaIgraOptions) : IAdvSenderFactory
{
    public ISingleHotRoleSender? Create(AdvertisementChannelInfo channel)
    {
        if (channel.Settings is not TelegramChannelSettings telegramSettings)
        {
            return null;
        }

        if (telegramSettings.ChatId.Id == 0)
        {
            return null;
        }

        return new TelegramSingleHotRoleSender(telegramSettings.ChatId, telegramNotificationService, kogdaIgraOptions.Value);
    }
}
