using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.UserProfile;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Games.Projects;

/// <summary>Роль и описание мастера, описание уже отрендерено из markdown с санитайзером.</summary>
public record ProjectMasterProfileViewModel(string Role, MarkupString? Description);

public record ProjectMasterCardViewModel(UserProfileDetailsViewModel Details, string Role, MarkupString? Description);

/// <summary>
/// Публичная страница мастеров проекта (ADR019, §7): публичные мастера — карточка профиля, роль, описание —
/// в порядке, заданном мастерами. Одинакова для всех: контакты в карточках — только публичные, как видит аноним,
/// даже если смотрящему (мастеру, со-мастеру, админу) доступно больше. Мастерам проекта внизу — ссылка на мастерскую страницу.
/// </summary>
/// <param name="ViewerIsMaster">Смотрит мастер проекта: показать ссылку на мастерскую страницу.</param>
public record ProjectMastersViewModel(
    ProjectIdentification ProjectId,
    IReadOnlyCollection<ProjectMasterCardViewModel> Masters,
    bool ViewerIsMaster)
{
    /// <param name="viewerId">Кто смотрит: мастеру проекта — ссылка на мастерскую страницу.</param>
    /// <param name="profiles">Профили действующих мастеров по UserId.</param>
    /// <param name="users">Пользователи — действующие мастера, по UserId.</param>
    /// <param name="cardBuilder">Строит карточки профиля мастеров.</param>
    public static ProjectMastersViewModel Build(
        ProjectInfo project,
        UserIdentification? viewerId,
        IReadOnlyDictionary<UserIdentification, ProjectMasterProfileViewModel> profiles,
        IReadOnlyDictionary<UserIdentification, UserInfo> users,
        UserProfileDetailsBuilder cardBuilder)
        // Профиль есть у каждого действующего мастера. Нет его (или пользователя) только у того, кого добавили
        // между двумя чтениями, — такой появится при следующей загрузке страницы.
        => new(
            project.ProjectId,
            [.. project.PublicMasters
                .Where(master => profiles.ContainsKey(master.UserId) && users.ContainsKey(master.UserId))
                .Select(master => new ProjectMasterCardViewModel(
                    cardBuilder.Build(users[master.UserId], currentUser: null),
                    profiles[master.UserId].Role,
                    profiles[master.UserId].Description))],
            project.HasMasterAccess(viewerId));
}
