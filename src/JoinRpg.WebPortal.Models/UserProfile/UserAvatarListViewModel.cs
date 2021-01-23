using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.UserProfile
{
#nullable enable
    public record UserAvatarListViewModel
        (IReadOnlyList<UserAvatarListItemViewModel> Avatars, string Email)
    {
        public UserAvatarListViewModel(User user) : this(
            Avatars:
                user
                    .Avatars
                    .Where(a => a.IsActive)
                    .Select(ua => new UserAvatarListItemViewModel(ua)).ToList(),
            Email: user.Email)
        {

        }
    }

    public record UserAvatarListItemViewModel(
        AvatarIdentification AvatarId,
        Uri AvatarUri,
        bool Selected)
    {
        public UserAvatarListItemViewModel(UserAvatar ua) : this(
                new AvatarIdentification(ua.UserAvatarId),
                new Uri(ua.Uri),
                ua.User.SelectedAvatar == ua)
        {

        }
    }
}
