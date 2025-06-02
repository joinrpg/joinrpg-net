using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
    (ProjectService projectService,
    IFieldSetupService fieldSetupService,
    IAccommodationService accommodationService,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectRepository projectRepository,
    ILogger<CreateProjectService> logger,
    CloneProjectHelperFactory cloneProjectHelperFactory
    ) : ICreateProjectService
{

    //TODO[Localize]
    async Task<CreateProjectResultBase> ICreateProjectService.CreateProject(CreateProjectRequest request)
    {
        DataModel.Project project;
        try
        {
            project = await projectService.AddProject(
                request.ProjectName,
                rootCharacterGroupName: "Все роли",
                cloneFrom: request is CloneProjectRequest cpr ? cpr.CopyFromId : null
                );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при создании проекта");
            return new FaildToCreateProjectResult(ex.Message);
        }

        var projectId = new ProjectIdentification(project.ProjectId);

        if (request is CloneProjectRequest cloneRequest)
        {
            try
            {
                if (await CopyFromAnother(cloneRequest, project, projectId))
                {
                    return new SuccessCreateProjectResult(projectId);
                }
                else
                {
                    return new PartiallySuccessCreateProjectResult(projectId, "Удалось скопировать не все элементы проекта");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при клонировании проекта");
                return new PartiallySuccessCreateProjectResult(projectId, ex.Message);
            }

        }
        var rootGroupId = new CharacterGroupIdentification(projectId, project.RootGroup.CharacterGroupId);

        try
        {
            switch (request.ProjectType)
            {
                case ProjectTypeDto.Larp:

                    await SetupLarp(request, projectId, rootGroupId);
                    break;
                case ProjectTypeDto.Convention:
                    await SetupConventionParticipant(request, projectId, rootGroupId);
                    break;
                case ProjectTypeDto.ConventionProgram:
                    await SetupConventionProgram(request, projectId, rootGroupId);
                    break;
                case ProjectTypeDto.EmptyProject:
                    // Ничего не делаем
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request.ProjectType));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при настройке проекта");
            return new PartiallySuccessCreateProjectResult(projectId, ex.Message);
        }

        return new SuccessCreateProjectResult(projectId);
    }
}
