using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.UserProfile;

public static class UserLinks
{
    [return: NotNullIfNotNull(nameof(user))]
    public static UserLinkViewModel? Create(User? user, ViewMode viewMode = ViewMode.Show)
    {
        return (user, viewMode) switch
        {
            (null, _) => null,
            (_, ViewMode.Hide) => UserLinkViewModel.Hidden,
            _ => new UserLinkViewModel(user.UserId, user.GetDisplayName().Trim(), viewMode),
        };
    }
}
