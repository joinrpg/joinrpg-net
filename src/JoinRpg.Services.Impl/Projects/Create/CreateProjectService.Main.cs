using JoinRpg.Services.Impl.Projects.Create;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Notification;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
    (ProjectService projectService,
    IFieldSetupService fieldSetupService,
    IAccommodationTypeService accommodationTypeService,
    ICharacterService characterService,
    IProjectMetadataRepository projectMetadataRepository,
    IProjectRepository projectRepository,
    IProjectRolesListService projectRolesListService,
    ILogger<CreateProjectService> logger,
    CloneProjectHelperFactory cloneProjectHelperFactory,
    IProjectPropsService projectPropsService,
    IAdminNotificationService adminNotificationService
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

        try
        {
            await HandleKogdaIgraChoice(request, projectId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при обработке статуса КогдаИгры");
        }

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

            // После шаблона: ошибка с поясом не должна оставить проект без настроек типа.
            if (request.TimeZone is { } timeZone)
            {
                await projectService.SetTimeZone(projectId, timeZone);
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
