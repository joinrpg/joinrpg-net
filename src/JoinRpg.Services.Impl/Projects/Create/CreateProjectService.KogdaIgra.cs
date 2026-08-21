using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
{
    //TODO[Localize]
    private async Task HandleKogdaIgraChoice(CreateProjectRequest request, ProjectIdentification projectId)
    {
        switch (request.KogdaIgraChoice)
        {
            case KogdaIgraLinkChoiceDto.Linked:
                // Автопривязки нет: только уведомляем админов с указанием выбранной игры;
                // саму привязку они подтверждают вручную через ProjectAdminControlPanel /
                // IKogdaIgraBindService.UpdateKogdaIgraBindings (единственная точка привязки).
                await adminNotificationService.NotifyAboutNewProjectKogdaIgraStatus(
                    projectId, request.ProjectName, request.KogdaIgraChoice, request.KogdaIgraGameId, message: null);
                break;
            case KogdaIgraLinkChoiceDto.NotOnKogdaIgra:
                await adminNotificationService.NotifyAboutNewProjectKogdaIgraStatus(
                    projectId, request.ProjectName, request.KogdaIgraChoice, gameId: null, request.MessageForKogdaIgraEditors);
                break;
            case KogdaIgraLinkChoiceDto.ShouldNotBeOnKogdaIgra:
            case KogdaIgraLinkChoiceDto.Trial:
                await projectPropsService.ChangeProjectProperties(
                    projectId, Permission.CanChangeProjectProperties, ProjectActiveRequirement.MustBeActive,
                    arguments: request.KogdaIgraChoice,
                    action: ctx => ctx.Project.Details.DisableKogdaIgraMapping = true);
                break;
        }
    }
}
