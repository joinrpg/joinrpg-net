using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DomainTypes.Posting;

namespace JoinRpg.Dal.Impl.Repositories;

internal class PostingChannelWriteRepository(MyDbContext ctx) : IPostingChannelWriteRepository
{
    public void CreateChannel(string name, TelegramChannelSettings settings) =>
        ctx.AdvertisementChannelsSet.Add(new AdvertisementChannelEntity
        {
            Name = name,
            ProjectId = null,
            Kind = PostingChannelKind.Telegram,
            SettingsJson = AdvertisementEntityMapper.SerializeSettings(settings),
            IsActive = true,
        });
}
