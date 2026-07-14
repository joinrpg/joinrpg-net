using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects.Create;

internal class CloneProjectHelperFactory(IProjectService projectService, ICharacterGroupService characterGroupService, IFieldSetupService fieldSetupService, IProjectMetadataRepository projectMetadataRepository, ICharacterService characterService, IPlotService plotService, IProjectRolesListService projectRolesListService, ILogger<CloneProjectHelper> logger)
{
    public CloneProjectHelper Create(CloneProjectRequest cloneRequest, Project project, ProjectIdentification projectId, ProjectInfo original, Project originalEntity)
    {
        return new CloneProjectHelper(cloneRequest, project, projectId, original, originalEntity, projectService, characterGroupService, fieldSetupService, projectMetadataRepository, characterService, plotService, projectRolesListService, logger);
    }
}
