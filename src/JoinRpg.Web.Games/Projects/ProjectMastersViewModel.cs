using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.UserProfile;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Games.Projects;

/// <summary>Роль и описание мастера, описание уже отрендерено из markdown с санитайзером.</summary>
public record ProjectMasterProfileViewModel(string Role, MarkupString? Description);

public record ProjectMasterCardViewModel(UserProfileDetailsViewModel Details, string Role, MarkupString? Description);

/// <summary>
/// Страница мастеров проекта для немастеров (ADR019, §7): публичные мастера — карточка профиля, роль, описание —
/// в порядке, заданном мастерами.
/// </summary>
public record ProjectMastersViewModel(IReadOnlyCollection<ProjectMasterCardViewModel> Masters)
{
    /// <param name="viewer">Кому показываем: контакты — по тем же правилам, что в профиле пользователя.</param>
    /// <param name="profiles">Профили действующих мастеров по UserId.</param>
    /// <param name="users">Пользователи — действующие мастера, по UserId.</param>
    public static ProjectMastersViewModel Build(
        ProjectInfo project,
        UserInfo? viewer,
        IReadOnlyDictionary<UserIdentification, ProjectMasterProfileViewModel> profiles,
        IReadOnlyDictionary<UserIdentification, UserInfo> users)
        // Профиль есть у каждого действующего мастера. Нет его (или пользователя) только у того, кого добавили
        // между двумя чтениями, — такой появится при следующей загрузке страницы.
        => new([.. project.PublicMasters
            .Where(master => profiles.ContainsKey(master.UserId) && users.ContainsKey(master.UserId))
            .Select(master => new ProjectMasterCardViewModel(
                new UserProfileDetailsViewModel(users[master.UserId], viewer),
                profiles[master.UserId].Role,
                profiles[master.UserId].Description))]);
}
