namespace JoinRpg.Web.Games.Projects;

public interface IProjectCreateClient
{
    Task<ProjectCreateResultViewModel> CreateProject(ProjectCreateViewModel model);

    Task<bool> IsProductionEnvironment();
}

public record ProjectCreateResultViewModel(ProjectIdentification? ProjectId, string? Error);
