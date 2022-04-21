using JoinRpg.Web.GameSubscribe;

namespace JoinRpg.Web.Models.Subscribe;

public record SubscribeByMasterPageViewModel
    (UserProfileDetailsViewModel UserDetails, SubscribeListViewModel SubscribeList)
{
}
