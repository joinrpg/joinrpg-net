using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes.Advertisement;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class AdvertisementDashboardModel(
    IAdvertisementChannelRepository channelRepository,
    IAdvertisementScheduleRepository scheduleRepository) : PageModel
{
    public IReadOnlyList<AdvertisementChannelInfo> Channels { get; private set; } = null!;
    public IReadOnlyList<AdvertisementScheduleInfo> Schedules { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Channels = [.. await channelRepository.GetAllChannels()];
        Schedules = [.. await scheduleRepository.GetActiveSchedules()];
    }
}
