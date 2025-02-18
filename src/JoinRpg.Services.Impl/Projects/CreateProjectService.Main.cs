using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
    (ProjectService projectService,
    IFieldSetupService fieldSetupService,
    IAccommodationService accommodationService,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectRepository projectRepository) : ICreateProjectService
{

    //TODO[Localize]
    async Task<ProjectIdentification> ICreateProjectService.CreateProject(CreateProjectRequest request)
    {
        var project = await projectService.AddProject(
            request.ProjectName,
            rootCharacterGroupName: "Все роли");

        var projectId = new ProjectIdentification(project.ProjectId);

        if (request is CloneProjectRequest cloneRequest)
        {
            await CopyFromAnother(request.ProjectName, project, projectId, cloneRequest.CopyFromId);
            return projectId;
        }

        switch (request.ProjectType)
        {
            case ProjectTypeDto.Larp:
                await SetupLarp(request, project, projectId);
                break;
            case ProjectTypeDto.Convention:
                await SetupConventionParticipant(request, project, projectId);
                break;
            case ProjectTypeDto.ConventionProgram:
                await SetupConventionProgram(request, project, projectId);
                break;
            case ProjectTypeDto.EmptyProject:
                // Ничего не делаем
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.ProjectType));
        }


        return projectId;
    }
}
