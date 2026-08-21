namespace JoinRpg.Web.Games.Projects;

public interface IProjectCreateClient
{
    Task<ProjectCreateResultViewModel> CreateProject(ProjectCreateViewModel model);
}

public record ProjectCreateResultViewModel(ProjectIdentification? ProjectId, string? Error);
