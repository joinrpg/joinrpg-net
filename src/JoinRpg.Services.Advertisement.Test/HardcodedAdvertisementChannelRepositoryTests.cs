using JoinRpg.Services.Advertisement.Channels;

namespace JoinRpg.Services.Advertisement.Test;

public class HardcodedAdvertisementChannelRepositoryTests
{
    [Fact]
    public async Task GetChannel_KnownChannelId_ReturnsTelegramChannel()
    {
        var repository = new HardcodedAdvertisementChannelRepository();

        var channel = await repository.GetChannel(HardcodedAdvertisementChannelRepository.HotRoleChannelId);

        channel.ShouldNotBeNull();
        channel.BoundProjectId.ShouldBeNull();
        var settings = channel.Settings.ShouldBeOfType<TelegramChannelSettings>();
        settings.ChatId.Id.ShouldBe(-1004315256401);
    }

    [Fact]
    public async Task GetChannel_UnknownChannelId_ReturnsNull()
    {
        var repository = new HardcodedAdvertisementChannelRepository();

        var channel = await repository.GetChannel(new AdvertisementChannelIdentification(999));

        channel.ShouldBeNull();
    }
}
