using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.Services.Interfaces.Notification;

public interface IAdminNotificationService
{
    Task SendTestMessage();

    /// <summary>
    /// Уведомляет всех сайт-админов о статусе нового проекта относительно КогдаИгры,
    /// выбранном мастером при создании проекта.
    /// </summary>
    /// <param name="gameId">Игра на КогдаИгре, если выбран <see cref="KogdaIgraLinkChoiceDto.Linked"/>.</param>
    /// <param name="message">Сообщение мастера редакторам КогдаИгры, если выбран <see cref="KogdaIgraLinkChoiceDto.NotOnKogdaIgra"/>.</param>
    Task NotifyAboutNewProjectKogdaIgraStatus(
        ProjectIdentification projectId, ProjectName projectName, KogdaIgraLinkChoiceDto choice,
        KogdaIgraIdentification? gameId, string? message);
}
