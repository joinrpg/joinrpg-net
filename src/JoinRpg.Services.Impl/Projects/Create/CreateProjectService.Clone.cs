using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
{
    private async Task<bool> CopyFromAnother(CloneProjectRequest cloneRequest, Project project, ProjectIdentification projectId)
    {
        var original = await projectMetadataRepository.GetProjectMetadata(cloneRequest.CopyFromId);
        var originalEntity = await projectRepository.GetProjectAsync(cloneRequest.CopyFromId);
        var helper = cloneProjectHelperFactory.Create(cloneRequest, project, projectId, original, originalEntity);
        return await helper.Clone();
    }

}
