using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Web.UserProfile;

/// <summary>
/// Единственный способ построить карточку пользователя (<see cref="UserProfileDetailsViewModel"/>).
/// Всё, что карточке нужно от сервера, она получает здесь, так что вызывающему нечего забыть дозаполнить.
/// </summary>
public class UserProfileDetailsBuilder
{
    /// <inheritdoc cref="UserProfileDetailsViewModel(UserInfo, UserInfo?)"/>
    public UserProfileDetailsViewModel Build(UserInfo user, UserInfo? currentUser) => new(user, currentUser);

    /// <inheritdoc cref="UserProfileDetailsViewModel(UserInfo, ProjectInfo, UserIdentification?, bool)"/>
    public UserProfileDetailsViewModel Build(UserInfo user, ProjectInfo currentProject, UserIdentification? currentUserId, bool viewAsAdmin)
        => new(user, currentProject, currentUserId, viewAsAdmin);
}
