using System.Diagnostics.CodeAnalysis;
using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.UserProfile;

public static class UserLinks
{

    [return: NotNullIfNotNull(nameof(user))]
    public static UserLinkViewModel? Create(UserInfo? user, ViewMode viewMode = ViewMode.Show)
    {
        return (user, viewMode) switch
        {
            (null, _) => null,
            (_, ViewMode.Hide) => UserLinkViewModel.Hidden,
            _ => new UserLinkViewModel(user.UserId, user.DisplayName.DisplayName, viewMode),
        };
    }

    [return: NotNullIfNotNull(nameof(user))]
    public static UserLinkViewModel? Create(UserInfoHeader? user, ViewMode viewMode = ViewMode.Show)
    {
        return (user, viewMode) switch
        {
            (null, _) => null,
            (_, ViewMode.Hide) => UserLinkViewModel.Hidden,
            _ => new UserLinkViewModel(user.UserId, user.DisplayName.DisplayName, viewMode),
        };
    }
}
