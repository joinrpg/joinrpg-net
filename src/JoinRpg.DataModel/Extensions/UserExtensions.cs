using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.DataModel.Extensions;

/// <summary>
/// Извлечение имени и заголовка пользователя из EF-сущности. Живёт в <c>JoinRpg.DataModel</c>,
/// а не в <c>JoinRpg.Domain</c>, потому что нужно и слою доступа к данным: <c>JoinRpg.Dal.Impl</c>
/// на <c>JoinRpg.Domain</c> не ссылается.
/// </summary>
public static class UserExtensions
{
    public static UserFullName ExtractFullName(this User user)
    {
        return new UserFullName(
            PrefferedName.FromOptional(user.PrefferedName),
            BornName.FromOptional(user.BornName),
            SurName.FromOptional(user.SurName),
            FatherName.FromOptional(user.FatherName));
    }

    public static UserDisplayName ExtractDisplayName(this User user) => new(user.ExtractFullName(), new Email(user.Email));

    public static UserInfoHeader ToUserInfoHeader(this User user)
        => new(new UserIdentification(user.UserId), user.ExtractDisplayName(), AvatarIdentification.FromOptional(user.SelectedAvatarId));
}
