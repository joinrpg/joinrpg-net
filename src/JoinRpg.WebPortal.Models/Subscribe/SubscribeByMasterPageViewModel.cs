using JoinRpg.Web.ProjectMasterTools.Subscribe;

namespace JoinRpg.Web.Models.Subscribe;

public record SubscribeByMasterPageViewModel
    (UserProfileDetailsViewModel UserDetails, SubscribeListViewModel SubscribeList)
{
}
