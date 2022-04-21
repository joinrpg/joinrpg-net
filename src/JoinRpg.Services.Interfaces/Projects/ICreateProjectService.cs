using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Projects;

public interface ICreateProjectService
{
    /// <summary>
    /// Create of new project
    /// </summary>
    Task<ProjectIdentification> CreateProject(CreateProjectRequest request);
}
