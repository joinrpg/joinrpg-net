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
    /// <summary>Письма легаси-канала, ушедшие за тест.</summary>
    private protected readonly FakeEmailService emailService = new();

    /// <summary>Общий журнал: сохранения и письма в порядке, в котором они случились.</summary>
    private protected readonly List<string> journal = [];

    protected AccommodationServiceTestBase()
    {
        mock.Project.Details.EnableAccommodation = true;
        unitOfWork.OnSaveChanges = _ => journal.Add("save");
        emailService.OnEmail = () => journal.Add("email");
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
                emailService,
                NullLogger<AccommodationPropsService>.Instance));
    }

    /// <summary>Переводит проект в архив — операции над ним запрещены.</summary>
    protected void ArchiveProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
    }

    protected AccommodationRoomIdentification RoomId(ProjectAccommodation room)
        => new(ProjectId, room.Id);

    protected AccommodationRequestIdentification GroupId(AccommodationRequest group)
        => new(ProjectId, group.Id);

    /// <summary>Идентификатор той же комнаты, но в чужом проекте.</summary>
    protected AccommodationRoomIdentification AlienRoomId(ProjectAccommodation room)
        => new(new ProjectIdentification(ProjectId.Value + 1), room.Id);
}
