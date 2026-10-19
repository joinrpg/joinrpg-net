using JoinRpg.DomainTypes.Users;
using JoinRpg.Web.UserProfile;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Games.Projects;

/// <summary>Роль и описание мастера, описание уже отрендерено из markdown с санитайзером.</summary>
public record ProjectMasterProfileViewModel(string Role, MarkupString? Description);

public record ProjectMasterCardViewModel(UserProfileDetailsViewModel Details, string Role, MarkupString? Description);

/// <summary>
/// Публичная страница мастеров проекта (ADR019, §7): публичные мастера — карточка профиля, роль, описание —
/// в порядке, заданном мастерами. Одинакова для всех; мастерам проекта внизу — ссылка на мастерскую страницу.
/// </summary>
/// <param name="ViewerIsMaster">Смотрит мастер проекта: показать ссылку на мастерскую страницу.</param>
public record ProjectMastersViewModel(
    ProjectIdentification ProjectId,
    IReadOnlyCollection<ProjectMasterCardViewModel> Masters,
    bool ViewerIsMaster)
{
    /// <param name="viewerId">Кто смотрит: мастеру проекта — ссылка на мастерскую страницу. Отдельно от
    /// <paramref name="viewer"/>, чтобы ссылка не зависела от того, загрузился ли профиль.</param>
    /// <param name="viewer">Профиль смотрящего: контакты — по тем же правилам, что в профиле пользователя.</param>
    /// <param name="profiles">Профили действующих мастеров по UserId.</param>
    /// <param name="users">Пользователи — действующие мастера, по UserId.</param>
    public static ProjectMastersViewModel Build(
        ProjectInfo project,
        UserIdentification? viewerId,
        UserInfo? viewer,
        IReadOnlyDictionary<UserIdentification, ProjectMasterProfileViewModel> profiles,
        IReadOnlyDictionary<UserIdentification, UserInfo> users)
        // Профиль есть у каждого действующего мастера. Нет его (или пользователя) только у того, кого добавили
        // между двумя чтениями, — такой появится при следующей загрузке страницы.
        => new(
            project.ProjectId,
            [.. project.PublicMasters
                .Where(master => profiles.ContainsKey(master.UserId) && users.ContainsKey(master.UserId))
                .Select(master => new ProjectMasterCardViewModel(
                    new UserProfileDetailsViewModel(users[master.UserId], viewer),
                    profiles[master.UserId].Role,
                    profiles[master.UserId].Description))],
            project.HasMasterAccess(viewerId));
}
