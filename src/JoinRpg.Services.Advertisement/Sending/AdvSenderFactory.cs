using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Advertisement.Sending;

internal class AdvSenderFactory(
    ITelegramNotificationService telegramNotificationService,
    IOptions<KogdaIgraOptions> kogdaIgraOptions,
    IUriLocator<ProjectIdentification> projectUriLocator) : IAdvSenderFactory
{
    public ISingleHotRoleSender? Create(PostingChannelInfo channel)
    {
        if (TryGetTelegramChatId(channel) is not { } chatId)
        {
            return null;
        }

        return new TelegramSingleHotRoleSender(chatId, telegramNotificationService, kogdaIgraOptions.Value);
    }

    public INewlyOpenedProjectsDigestSender? CreateNewlyOpenedProjectsDigestSender(PostingChannelInfo channel)
    {
        if (TryGetTelegramChatId(channel) is not { } chatId)
        {
            return null;
        }

        return new TelegramNewlyOpenedProjectsDigestSender(chatId, telegramNotificationService, projectUriLocator);
    }

    private static TelegramChatId? TryGetTelegramChatId(PostingChannelInfo channel) =>
        channel.Settings switch
        {
            TelegramChannelSettings { ChatId.Id: 0 } => null,
            TelegramChannelSettings telegramSettings => telegramSettings.ChatId,
        };
}
