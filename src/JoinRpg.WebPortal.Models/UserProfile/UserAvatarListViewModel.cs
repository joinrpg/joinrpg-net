using JoinRpg.DataModel;
using JoinRpg.DataModel.Users;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Models.UserProfile;

#nullable enable
public record UserAvatarListViewModel
    (IReadOnlyList<UserAvatarListItemViewModel> Avatars, string Email, int UserId)
{
    public UserAvatarListViewModel(User user) : this(
        Avatars:
            user
                .Avatars
                .Where(a => a.IsActive)
                .Select(ua => new UserAvatarListItemViewModel(ua)).ToList(),
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
    public UserAvatarListItemViewModel(UserAvatar ua) : this(
            new AvatarIdentification(ua.UserAvatarId),
            new Uri(ua.CachedUri ?? ua.OriginalUri),
            ua.User.SelectedAvatar == ua,
            GetSourceLabel(ua)
        )
    {

    }

    private static string GetSourceLabel(UserAvatar ua)
    {
        return ua.AvatarSource switch
        {
            UserAvatar.Source.GrAvatar => "Сервис gravatar.com",
            UserAvatar.Source.SocialNetwork => "Социальная сеть " + ua.ProviderId,
            _ => "Неизвестный источник аватарки",
        };
    }
}
