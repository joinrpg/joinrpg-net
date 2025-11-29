using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal class CloneProjectHelperFactory(IProjectService projectService, IFieldSetupService fieldSetupService, IProjectMetadataRepository projectMetadataRepository, ICharacterService characterService, IPlotService plotService, ILogger<CloneProjectHelper> logger)
{
    public CloneProjectHelper Create(CloneProjectRequest cloneRequest, Project project, ProjectIdentification projectId, ProjectInfo original, Project originalEntity)
    {
        return new CloneProjectHelper(cloneRequest, project, projectId, original, originalEntity, projectService, fieldSetupService, projectMetadataRepository, characterService, plotService, logger);
    }
}
