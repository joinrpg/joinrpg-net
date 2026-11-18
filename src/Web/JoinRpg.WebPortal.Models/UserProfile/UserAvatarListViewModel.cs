using JoinRpg.DataModel.Users;

namespace JoinRpg.Web.Models.UserProfile;

#nullable enable
public record UserAvatarListViewModel
    (IReadOnlyList<UserAvatarListItemViewModel> Avatars, string Email)
{
    public UserAvatarListViewModel(IEnumerable<UserAvatar> avatars, AvatarIdentification? selectedAvatar, string email, IAvatarUriResolver avatarUriResolver) : this(
        Avatars:
            avatars
                .Where(a => a.IsActive)
                .Select(ua => new UserAvatarListItemViewModel(ua, selectedAvatar, avatarUriResolver)).ToList(),
        Email: email
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
    public UserAvatarListItemViewModel(UserAvatar ua, AvatarIdentification? selectedAvatar, IAvatarUriResolver avatarUriResolver) : this(
            new AvatarIdentification(ua.UserAvatarId),
            avatarUriResolver.GetAvatarUri(ua.CachedUri, ua.OriginalUri),
            selectedAvatar?.Value == ua.UserAvatarId,
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
