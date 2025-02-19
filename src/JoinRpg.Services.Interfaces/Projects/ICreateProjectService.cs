namespace JoinRpg.Services.Interfaces.Projects;

public interface ICreateProjectService
{
    /// <summary>
    /// Create of new project
    /// </summary>
    Task<CreateProjectResultBase> CreateProject(CreateProjectRequest request);
}
