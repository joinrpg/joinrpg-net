using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Test.Projects;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Общая обвязка тестов поселения (ADR018): сервис собран на реальном
/// <see cref="AccommodationPropsService"/> и фейковом write-хэндле поверх <c>MockedProject</c>.
/// </summary>
public abstract class AccommodationServiceTestBase : ProjectMetadataServiceTestBase
{
    /// <summary>Уведомления о проживании, ушедшие за тест.</summary>
    private protected readonly FakeAccommodationNotificationService notificationService = new();

    /// <summary>Общий журнал: сохранения и уведомления в порядке, в котором они случились.</summary>
    private protected readonly List<string> journal = [];

    protected AccommodationServiceTestBase()
    {
        mock.Project.Details.EnableAccommodation = true;
        unitOfWork.OnSaveChanges = _ => journal.Add("save");
        notificationService.OnNotification = () => journal.Add("notification");
    }

    private protected AccommodationServiceImpl CreateService(int? currentUserId = null, bool isAdmin = false)
    {
        var currentUser = CreateCurrentUser(currentUserId, isAdmin);
        return new AccommodationServiceImpl(
            unitOfWork,
            currentUser,
            metadataRepository,
            new AccommodationPropsService(
                unitOfWork,
                currentUser,
                metadataRepository,
                notificationService,
                NullLogger<AccommodationPropsService>.Instance));
    }

    /// <summary>Переводит проект в архив — операции над ним запрещены.</summary>
    protected void ArchiveProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
    }

    /// <summary>Идентификатор той же комнаты, но в чужом проекте.</summary>
    protected AccommodationRoomIdentification AlienRoomId(ProjectAccommodation room)
        => new(new ProjectIdentification(ProjectId.Value + 1), room.Id);
}
