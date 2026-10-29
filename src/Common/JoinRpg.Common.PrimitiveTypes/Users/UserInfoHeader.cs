namespace JoinRpg.Common.PrimitiveTypes.Users;

/// <param name="UserId">Идентификатор пользователя</param>
/// <param name="DisplayName">Отображаемое имя пользователя</param>
/// <param name="Avatar">Идентификатор выбранной аватарки, null если у пользователя её нет. Аватарка неизменяема по идентификатору.</param>
public record class UserInfoHeader(UserIdentification UserId, UserDisplayName DisplayName, AvatarIdentification? Avatar);
