namespace JoinRpg.Web.ProjectCommon.Projects;

public interface IProjectCreateClient
{
    Task<ProjectCreateResultViewModel> CreateProject(ProjectCreateViewModel model);
}

public record ProjectCreateResultViewModel(ProjectIdentification? ProjectId, string? Error);
