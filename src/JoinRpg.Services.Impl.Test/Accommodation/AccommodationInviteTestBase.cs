using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Services.Impl.Accommodation;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Общая обвязка тестов контура приглашений к совместному проживанию
/// (<c>AccommodationInviteServiceImpl</c>).
/// </summary>
/// <remarks>
/// Сервис ещё не переехал на write-репозитории и ходит в <c>UnitOfWork.GetDbSet</c>, поэтому
/// наборы подключены к <see cref="FakeUnitOfWork"/> явно (см. <c>UseDbSet</c>).
/// </remarks>
public abstract class AccommodationInviteTestBase
{
    private protected readonly MockedProject mock = new();
    private protected readonly FakeUnitOfWork unitOfWork;
    private protected readonly FakeAccommodationNotificationService notificationService = new();

    /// <summary>Тип проживания по умолчанию: мест хватает на всех участников тестов.</summary>
    private protected readonly ProjectAccommodationType accommodationType;

    /// <summary>Сохранения и уведомления в порядке, в котором они случились.</summary>
    private protected readonly List<string> journal = [];

    protected AccommodationInviteTestBase()
    {
        mock.Project.Details.EnableAccommodation = true;
        accommodationType = mock.CreateAccommodationType("Домик", capacity: 4);
        mock.ReInitProjectInfo();

        unitOfWork = new FakeUnitOfWork(mock);
        unitOfWork.UseDbSet(mock.Project.Claims);
        unitOfWork.UseDbSet(mock.AccommodationRequests);
        unitOfWork.UseDbSet(mock.AccommodationInvites);

        unitOfWork.OnSaveChanges = _ => journal.Add("save");
        notificationService.OnNotification = () => journal.Add("notification");
    }

    /// <summary>
    /// Сервис от лица указанного пользователя; по умолчанию — от мастера проекта, у которого есть
    /// все права.
    /// </summary>
    private protected AccommodationInviteServiceImpl CreateService(int? currentUserId = null)
        => new(unitOfWork, notificationService, new FakeCurrentUserAccessor(currentUserId ?? mock.Master.UserId));

    /// <summary>Заявка игрока с выбранным типом проживания (своя заявка на проживание).</summary>
    private protected Claim CreateClaimWithAccommodation(string characterName)
    {
        var claim = CreateClaim(characterName);
        _ = mock.CreateAccommodationRequest(accommodationType, claim);
        return claim;
    }

    private protected Claim CreateClaim(string characterName)
        => mock.CreateApprovedClaim(mock.CreateCharacter(characterName), mock.Player);

    private protected AccommodationRequestIdentification RequestId(Claim claim)
        => new(mock.ProjectInfo.ProjectId, claim.AccommodationRequest!.Id);

    private protected AccommodationRequestIdentification RequestId(AccommodationRequest request)
        => new(mock.ProjectInfo.ProjectId, request.Id);

    private protected AccommodationInviteIdentification InviteId(AccommodationInvite invite)
        => new(mock.ProjectInfo.ProjectId, invite.Id);

    /// <summary>Единственное ушедшее уведомление о приглашении.</summary>
    private protected AccommodationInviteNotification Notification()
        => notificationService.Invites.ShouldHaveSingleItem();

    private protected IReadOnlyCollection<int> RecipientClaimIds()
        => [.. Notification().RecipientClaims.Select(claimId => claimId.ClaimId)];

    /// <summary>
    /// Уведомление обязано уйти после всех сохранений: до <c>SaveChanges</c> операции в базе ещё
    /// нет, а откатить отправленное письмо невозможно.
    /// </summary>
    private protected void NotificationWentAfterSave()
    {
        journal.ShouldContain("save");
        journal.IndexOf("notification").ShouldBeGreaterThan(journal.LastIndexOf("save"));
    }
}
