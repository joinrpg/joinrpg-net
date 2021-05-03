using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Models.UserProfile
{
    public record UserLinkViewModel(
        int UserId,
        string DisplayName)
    {
        public UserLinkViewModel(User user) : this(user.UserId, user.GetDisplayName().Trim())
        {

        }

        public static UserLinkViewModel? FromOptional(User user)
            => user is null ? null : new UserLinkViewModel(user);
    }
}
