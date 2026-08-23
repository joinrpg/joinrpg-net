using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Games.Projects;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.WebPortal.Managers.Projects;

internal class ProjectCreateViewService(
    ICreateProjectService createProjectService,
    IHostEnvironment hostEnvironment,
    ILogger<ProjectCreateViewService> logger
    ) : IProjectCreateClient
{
    public Task<bool> IsProductionEnvironment() => Task.FromResult(hostEnvironment.IsProduction());

    public async Task<ProjectCreateResultViewModel> CreateProject(ProjectCreateViewModel model)
    {
        if (model.KogdaIgraChoice == KogdaIgraLinkChoiceViewModel.Trial && hostEnvironment.IsProduction())
        {
            return new ProjectCreateResultViewModel(null, "Пробные проекты нельзя создавать на боевом сайте");
        }

        try
        {
            var request = CreateProjectRequest.Create(new ProjectName(model.ProjectName),
                (ProjectTypeDto)model.ProjectType,
                model.CopyFromProjectId,
                (ProjectCopySettingsDto)model.CopySettings,
                (KogdaIgraLinkChoiceDto)model.KogdaIgraChoice,
                model.KogdaIgraGameId,
                model.MessageForKogdaIgraEditors
                );
            var result = await createProjectService.CreateProject(request);

            return result switch
            {
                FaildToCreateProjectResult failed => new ProjectCreateResultViewModel(null, failed.Message),
                PartiallySuccessCreateProjectResult partially => new ProjectCreateResultViewModel(partially.ProjectId, $"Ошибка: {partially.Message}"),
                SuccessCreateProjectResult success => new ProjectCreateResultViewModel(success.ProjectId, Error: null),
                _ => new ProjectCreateResultViewModel(null, "Неизвестный результат создания проекта"),
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error creating project");
            return new ProjectCreateResultViewModel(null, exception.Message);
        }
    }
}
