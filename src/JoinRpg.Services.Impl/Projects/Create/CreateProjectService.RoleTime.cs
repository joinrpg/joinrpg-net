using JoinRpg.Services.Interfaces.ProjectAccess;

namespace JoinRpg.Services.Impl.Projects;

internal partial class CreateProjectService
{
    //TODO[Localize]
    /// <summary>
    /// Робот roletime.ru забирает расписание программы в «Расписание конвентов». Мастером без прав и
    /// скрытым от игроков: организаторам, которым экспорт не нужен, достаточно его удалить.
    /// </summary>
    private async Task AddRoleTimeRobot(ProjectIdentification projectId)
    {
        if (roleTimeOptions.Value.RobotUserId is not { } robotUserId)
        {
            return;
        }
        await projectAccessService.GrantAccess(new GrantAccessRequest
        {
            ProjectId = projectId,
            UserId = robotUserId,
            Permissions = [],
            Role = new("Робот: синхронизация с roletime.ru"),
            Description = new MarkdownString("Робот, который нужен для экспорта расписания в [«Расписание конвентов»](https://roletime.ru). Если он вам не нужен, удалите."),
            IsPublic = false,
        });
    }
}
