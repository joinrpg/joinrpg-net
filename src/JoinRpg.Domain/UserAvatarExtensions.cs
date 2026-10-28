using JoinRpg.DataModel.Users;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain;

public static class UserAvatarExtensions
{
    /// <summary>
    /// Заводит пользователю аватарку Gravatar по его email и выбирает её, если выбранной ещё нет.
    /// Новому пользователю её надо добавлять отдельным сохранением, после того как он сам записан в базу:
    /// за один SaveChanges EF6 не вставит пользователя и аватарку, которые ссылаются друг на друга.
    /// </summary>
    public static UserAvatar AddGravatar(this User user)
    {
        // Не кешируем, иначе Gravatar перестанет обновляться сам.
        var avatar = new UserAvatar()
        {
            AvatarSource = AvatarSource.GrAvatar,
            IsActive = true,
            ProviderId = null,
            OriginalUri = GravatarHelper.GetLink(user.Email, GravatarHelper.DefaultAvatarSize).AbsoluteUri,
            CachedUri = null,
            User = user,
        };
        user.Avatars.Add(avatar);

        if (user.SelectedAvatarId is null && user.SelectedAvatar is null)
        {
            user.SelectedAvatar = avatar;
        }

        return avatar;
    }

    /// <summary>
    /// Выбирает аватарку по умолчанию пользователю, у которого выбранной нет: активный Gravatar, если он уже заведён,
    /// иначе заводит новый. Пользователя с выбранной аватаркой не трогает, поэтому повторный вызов ничего не добавляет.
    /// Аватарки пользователя должны быть загружены. Пользователь уже должен быть записан в базу (см. <see cref="AddGravatar"/>).
    /// </summary>
    public static void EnsureDefaultAvatar(this User user)
    {
        if (user.SelectedAvatarId is not null || user.SelectedAvatar is not null)
        {
            return;
        }

        if (user.Avatars.FirstOrDefault(a => a.AvatarSource == AvatarSource.GrAvatar && a.IsActive) is UserAvatar grAvatar)
        {
            user.SelectedAvatar = grAvatar;
        }
        else
        {
            _ = user.AddGravatar();
        }
    }

    /// <summary>
    /// Добавляет аватарку из соцсети. Выбирает её, если выбранной нет, выбранная уже неактивна (соцсеть сменила
    /// фотографию) или выбран Gravatar, который заводится по умолчанию, а аватарок из соцсетей у пользователя
    /// ещё не было. Если пользователь уже выбирал между ними — его выбор не трогаем.
    /// </summary>
    public static void AddSocialAvatar(this User user, UserAvatar avatar)
    {
        var selectNew = user.SelectedAvatar is null
            || !user.SelectedAvatar.IsActive
            || (user.SelectedAvatar.AvatarSource == AvatarSource.GrAvatar
                && !user.Avatars.Any(a => a.AvatarSource == AvatarSource.SocialNetwork));

        user.Avatars.Add(avatar);

        if (selectNew)
        {
            user.SelectedAvatar = avatar;
        }
    }
}
