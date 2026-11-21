using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Projects;

internal class MasterEmailService(
    MastersNotificationService mastersNotificationService,
    IProjectMetadataRepository projectMetadataRepository,
    IVirtualUsersService virtualUsersService
    )
{

    public async Task EmailProjectStale(ProjectStaleMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var subject = $"{metadata.ProjectName.Value}: проект будет закрыт из-за неактивности";

        var body = $@"
Проект {metadata.ProjectName.Value} был в последний раз активен {email.LastActiveDate:yyyy-MM-dd}. Если до {email.WillCloseDate:yyyy-MM-dd} активность в нем не появится, он автоматически будет закрыт.

Подробности в справке https://docs.joinrpg.ru/project/after.html#auto-close

Не переживайте, закрытый проект всегда можно будет посмотреть, он не пропадет. Если проект завершен или больше не нужен, вы можете закрыть его сами.";
        await mastersNotificationService.NotifyAllMasters(metadata, subject, body, virtualUsersService.RobotUserId);
    }

    public async Task EmailProjectClosed(ProjectClosedMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"Проект {metadata.ProjectName.Value} был закрыт.";
        var subject = $"{metadata.ProjectName.Value}: проект закрыт";
        await mastersNotificationService.NotifyAllMasters(metadata, subject, body, email.Initiator);
    }

    /// <summary>
    /// Снятие блокировки необратимо: восстановление больше недоступно, заблокировать снова нельзя (ADR023).
    /// Поэтому узнать должны все мастера проекта и все админы сайта.
    /// </summary>
    // TODO[Localize]
    public async Task EmailProjectUnblocked(ProjectUnblockedMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"{email.Initiator.DisplayName.DisplayName} снял(а) блокировку с проекта {metadata.ProjectName.Value}.

Восстановление данных в проекте больше недоступно, заблокировать проект снова нельзя.";
        var subject = $"{metadata.ProjectName.Value}: блокировка снята";

        await mastersNotificationService.NotifyMastersAndAdmins(metadata, subject, body, email.Initiator.UserId);
    }

    /// <summary>
    /// Еженедельное напоминание мастерам заблокированного проекта определиться с его судьбой (ADR023).
    /// </summary>
    // TODO[Localize]
    public async Task EmailProjectBlockedReminder(ProjectBlockedMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"Ваш проект {metadata.ProjectName.Value} все еще заблокирован после утери данных в октябре. Вам нужно определиться, что делать с вашим проектом. Если он вам больше не нужен, закройте его. Если нужен — воспользуйтесь инструкцией по восстановлению: https://docs.joinrpg.ru/recovery-2026.html. По любым вопросам пишите в https://joinrpg.ru/support";
        var subject = $"{metadata.ProjectName.Value}: проект заблокирован на время восстановления данных";

        await mastersNotificationService.NotifyAllMasters(metadata, subject, body, virtualUsersService.RobotUserId);
    }

    /// <summary>
    /// Алерт мастерам, ответственным за финансы проекта: в бюджете не зафиксированы расходы более 5 недель.
    /// </summary>
    // TODO[Localize]
    public async Task EmailBudgetExpensesAlert(BudgetExpensesAlertMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"В проекте {metadata.ProjectName.Value} не зафиксированы расходы уже более 5 недель. Если расходы были — проверьте, что они внесены в бюджет проекта: https://joinrpg.ru/{email.ProjectId.Value}/money/expenses";
        var subject = $"{metadata.ProjectName.Value}: в бюджете не зафиксированы расходы более 5 недель";

        await mastersNotificationService.NotifyMastersWithPermission(metadata, Permission.CanManageMoney, subject, body, virtualUsersService.RobotUserId);
    }

    public async Task EmailProjectClosedStale(ProjectClosedStaleMail email)
    {
        var metadata = await projectMetadataRepository.GetProjectMetadata(email.ProjectId);

        var body = $@"Проект {metadata.ProjectName.Value} был закрыт, т.к. он не был активен с {email.LastActiveDate:yyyy-MM-dd}.";
        var subject = $"{metadata.ProjectName.Value}: проект закрыт";
        await mastersNotificationService.NotifyAllMasters(metadata, subject, body, virtualUsersService.RobotUserId);
    }
}
