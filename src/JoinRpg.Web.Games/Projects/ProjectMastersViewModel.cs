namespace JoinRpg.Web.Games.Projects;

/// <summary>
/// Страница мастеров проекта для немастеров (ADR019). Только то, что и так видно на главной странице проекта.
/// </summary>
public record ProjectMastersViewModel(IReadOnlyCollection<UserLinkViewModel> Masters)
{
    /// <param name="viewer">Кому показываем: непубличных мастеров видят только мастера проекта (ADR019, §3).</param>
    public static ProjectMastersViewModel Build(ProjectInfo project, UserIdentification? viewer)
        => new([.. project.GetMastersVisibleTo(viewer).Select(master => new UserLinkViewModel(master.UserInfo))]);
}
