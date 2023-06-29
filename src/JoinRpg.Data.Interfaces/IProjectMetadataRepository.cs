using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Data.Interfaces;

public interface IProjectMetadataRepository
{
    Task<ProjectInfo> GetProjectMetadata(ProjectIdentification projectId);
}
