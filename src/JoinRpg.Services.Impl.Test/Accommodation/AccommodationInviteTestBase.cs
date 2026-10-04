using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Test.Claims;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Общая обвязка тестов контура приглашений к совместному проживанию
/// (<c>AccommodationInviteServiceImpl</c>).
/// </summary>
/// <remarks>
/// Наследует обвязку claim-контура (ADR014): корень агрегата у приглашения — заявка, поэтому
/// сервису нужен настоящий <c>CharacterPropsService</c> поверх тех же фейков. Методы, которые на
/// props-сервис ещё не переехали, ходят в <c>UnitOfWork.GetDbSet</c>, поэтому наборы подключены к
/// <see cref="FakeUnitOfWork"/> явно (см. <c>UseDbSet</c>).
/// </remarks>
public abstract class AccommodationInviteTestBase : ClaimServiceTestBase
{
    /// <summary>Тип проживания по умолчанию: мест хватает на всех участников тестов.</summary>
    private protected readonly ProjectAccommodationType accommodationType;

    /// <summary>Сохранения и уведомления в порядке, в котором они случились.</summary>
    private protected readonly List<string> journal = [];

    protected AccommodationInviteTestBase()
    {
        mock.Project.Details.EnableAccommodation = true;
        accommodationType = mock.CreateAccommodationType("Домик", capacity: 4);
        mock.ReInitProjectInfo();

        unitOfWork.UseDbSet(mock.Project.Claims);
        unitOfWork.UseDbSet(mock.AccommodationRequests);
        unitOfWork.UseDbSet(mock.AccommodationInvites);

        OnSaveChanges = _ => journal.Add("save");
        notificationService.OnNotification = () => journal.Add("notification");
    }

    /// <summary>Канал уведомлений о проживании — в нём видно и приглашения.</summary>
    private protected FakeAccommodationNotificationService notificationService => accommodationNotifications;

    /// <summary>
    /// Сервис от лица указанного пользователя; по умолчанию — от мастера проекта, у которого есть
    /// все права.
    /// </summary>
    private protected AccommodationInviteServiceImpl CreateService(int? currentUserId = null)
        => new(
            unitOfWork,
            notificationService,
            CreatePropsService(currentUserId),
            new FakeAccommodationInviteRepository(mock),
            CreateCurrentUser(currentUserId));

    /// <summary>Переводит проект в архив — операции над ним запрещены.</summary>
    private protected void ArchiveProject()
    {
        mock.Project.Active = false;
        mock.Project.IsAcceptingClaims = false;
        mock.ReInitProjectInfo();
    }

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
