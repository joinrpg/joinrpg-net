namespace JoinRpg.Web.Games.Projects;

/// <summary>
/// Страница мастеров проекта для немастеров (ADR019). Только то, что и так видно на главной странице проекта.
/// </summary>
public record ProjectMastersViewModel(IReadOnlyCollection<UserLinkViewModel> Masters)
{
    public static ProjectMastersViewModel Build(ProjectInfo project)
        => new([.. project.Masters.Select(master => new UserLinkViewModel(master.UserInfo))]);
}
