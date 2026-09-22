using System.Text.Json.Serialization;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.PrimitiveTypes.Users;

namespace JoinRpg.Common.WebComponents;

// JsonConstructor нужен, потому что конструкторов стало два, и System.Text.Json не умеет выбирать сам.
[method: JsonConstructor]
public record UserLinkViewModel(UserIdentification UserId, string DisplayName, ViewMode ViewMode)
{
    public static UserLinkViewModel Hidden = new(new UserIdentification(-1), "скрыто", ViewMode.Hide);

    /// <summary>
    /// В режиме <see cref="ViewMode.Hide"/> данные пользователя не попадают в модель вообще.
    /// </summary>
    public UserLinkViewModel(UserInfoHeader user, ViewMode viewMode = ViewMode.Show)
        : this(
            viewMode == ViewMode.Hide ? Hidden.UserId : user.UserId,
            viewMode == ViewMode.Hide ? Hidden.DisplayName : user.DisplayName.DisplayName,
            viewMode)
    {
    }
}
