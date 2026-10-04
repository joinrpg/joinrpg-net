using Microsoft.AspNetCore.Components;

namespace JoinRpg.Web.Games.Projects;

/// <summary>Роль и описание мастера, описание уже отрендерено из markdown с санитайзером.</summary>
public record ProjectMasterProfileViewModel(string Role, MarkupString? Description);

public record ProjectMasterCardViewModel(UserLinkViewModel User, string Role, MarkupString? Description);

/// <summary>
/// Страница мастеров проекта для немастеров (ADR019, §7): имя, роль, описание — в порядке, заданном мастерами.
/// </summary>
public record ProjectMastersViewModel(IReadOnlyCollection<ProjectMasterCardViewModel> Masters)
{
    /// <param name="viewer">Кому показываем: непубличных мастеров видят только мастера проекта (ADR019, §3).</param>
    /// <param name="profiles">Профили действующих мастеров по UserId.</param>
    public static ProjectMastersViewModel Build(
        ProjectInfo project,
        UserIdentification? viewer,
        IReadOnlyDictionary<UserIdentification, ProjectMasterProfileViewModel> profiles)
        // Профиль есть у каждого действующего мастера. Нет его только у того, кого добавили между двумя чтениями, —
        // такой появится при следующей загрузке страницы.
        => new([.. project.GetMastersVisibleTo(viewer)
            .Where(master => profiles.ContainsKey(master.UserId))
            .Select(master => new ProjectMasterCardViewModel(
                new UserLinkViewModel(master.UserInfo),
                profiles[master.UserId].Role,
                profiles[master.UserId].Description))]);
}
