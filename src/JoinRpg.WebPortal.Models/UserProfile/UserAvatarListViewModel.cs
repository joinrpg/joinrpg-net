using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;

namespace JoinRpg.Web.Models.UserProfile;

#nullable enable
public record UserAvatarListViewModel
    (IReadOnlyList<UserAvatarListItemViewModel> Avatars, string Email, int UserId)
{
    public UserAvatarListViewModel(User user, IAvatarUriResolver avatarUriResolver) : this(
        Avatars:
            user
                .Avatars
                .Where(a => a.IsActive)
                .Select(ua => new UserAvatarListItemViewModel(ua, avatarUriResolver)).ToList(),
        Email: user.Email,
        UserId: user.UserId
        )
    {

    }
}

public record UserAvatarListItemViewModel(
    AvatarIdentification AvatarId,
    Uri AvatarUri,
    bool Selected,
    string Source)
{
    public UserAvatarListItemViewModel(UserAvatar ua, IAvatarUriResolver avatarUriResolver) : this(
            new AvatarIdentification(ua.UserAvatarId),
            avatarUriResolver.GetAvatarUri(ua.CachedUri, ua.OriginalUri),
            ua.User.SelectedAvatar == ua,
            GetSourceLabel(ua)
        )
    {

    }

    private static string GetSourceLabel(UserAvatar ua)
    {
        return ua.AvatarSource switch
        {
            AvatarSource.GrAvatar => "Сервис gravatar.com",
            AvatarSource.SocialNetwork => "Социальная сеть " + ua.ProviderId,
            _ => "Неизвестный источник аватарки",
        };
    }
}
