using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.WebComponents;

namespace JoinRpg.Web.Models.UserProfile;

public static class UserLinks
{
    [return: NotNullIfNotNull("user")]
    public static UserLinkViewModel? Create(User? user)
    {
        return user is null ?
            null : new UserLinkViewModel(user.UserId, user.GetDisplayName().Trim());
    }
}
