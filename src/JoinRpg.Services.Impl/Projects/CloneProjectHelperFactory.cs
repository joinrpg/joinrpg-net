using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl.Projects;

internal class CloneProjectHelperFactory(IProjectService projectService, IFieldSetupService fieldSetupService, IProjectMetadataRepository projectMetadataRepository, ICharacterService characterService, IPlotService plotService, ILogger<CloneProjectHelper> logger)
{
    public CloneProjectHelper Create(CloneProjectRequest cloneRequest, Project project, ProjectIdentification projectId, ProjectInfo original, Project originalEntity)
    {
        return new CloneProjectHelper(cloneRequest, project, projectId, original, originalEntity, projectService, fieldSetupService, projectMetadataRepository, characterService, plotService, logger);
    }
}
