using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Mocks;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Тесты сервиса уведомлений о проживании: кто получает уведомление и что в нём написано.
/// Текст целиком закрыт снапшотами в <see cref="VerifyAccommodationNotificationTest"/>, здесь —
/// то, чего снапшот не видит: состав получателей, порядок имён и работа с выборкой заявок.
/// </summary>
public class AccommodationNotificationServiceTest
{
    private readonly MockedProject mock = new();
    private readonly FakeNotificationService notificationService = new();
    private readonly FakeClaimsRepository claimsRepository;
    private readonly FakeUserSubscribeRepository subscribeRepository = new();
    private readonly ProjectAccommodationType accommodationType;

    /// <summary>Инициатор операции — мастер проекта.</summary>
    private readonly UserInfoHeader initiator;

    public AccommodationNotificationServiceTest()
    {
        claimsRepository = new FakeClaimsRepository(mock);
        mock.Project.Details.EnableAccommodation = true;
        accommodationType = mock.CreateAccommodationType("Домик");
        mock.ReInitProjectInfo();
        initiator = mock.Master.ToUserInfoHeader();
    }

    private AccommodationNotificationService CreateService()
        => new(
            notificationService,
            new FakeProjectMetadataRepository(mock),
            claimsRepository,
            new SubscribeCalculator(subscribeRepository, new FakeCharacterInfoRepository(mock), claimsRepository),
            new AccommodationNotificationTextBuilder());

    /// <summary>
    /// Заявка, известная и моку (персонаж для расчёта подписок), и репозиторию заголовков.
    /// </summary>
    /// <param name="playerId">Игрок заявки. Отдельным пользователем в моке его заводить незачем:
    /// сервис видит игрока только через заголовок заявки.</param>
    /// <param name="responsibleMaster">Ответственный мастер; по умолчанию — <c>mock.Master</c>.</param>
    private ClaimWithPlayer AddClaim(string characterName, int playerId, User? responsibleMaster = null)
    {
        var character = mock.CreateCharacter(characterName);
        var claim = mock.CreateClaim(character, mock.Player);
        var header = new ClaimWithPlayer
        {
            ClaimId = claim.GetId(),
            CharacterName = characterName,
            Player = new UserInfoHeader(new UserIdentification(playerId), new UserDisplayName($"Игрок{playerId}", null)),
            ExtraNicknames = null,
            ResponsibleMasterUserId = new UserIdentification((responsibleMaster ?? mock.Master).UserId),
            CharacterId = character.GetId(),
        };
        claimsRepository.Headers.Add(header);
        return header;
    }

    private RoomOccupancyNotification Notification(
        IReadOnlyCollection<ClaimIdentification> changed,
        IReadOnlyCollection<ClaimIdentification> remaining,
        UserInfoHeader? initiatorOverride = null)
        => new(
            new AccommodationRoomIdentification(mock.ProjectInfo.ProjectId, 17),
            RoomName: "101",
            accommodationType.GetId(),
            initiatorOverride ?? initiator,
            changed,
            remaining,
            RoomOccupancyChangeKind.Occupied);

    private NotificationEvent Queued() => notificationService.Queued.Single();

    private IReadOnlyCollection<UserIdentification> QueuedRecepients()
        => [.. Queued().Recepients.Select(r => r.UserId)];

    [Fact]
    public async Task InitiatorIsNotRecepientEvenWhenHeIsPlayer()
    {
        // Инициатор — игрок одной из заявок комнаты: отсеять его должен SubscribeCalculator.
        var own = AddClaim("Свой", playerId: 1);
        var neighbour = AddClaim("Сосед", playerId: 3);

        await CreateService().SendNotification(
            Notification([own.ClaimId], [neighbour.ClaimId], initiatorOverride: own.Player));

        QueuedRecepients().ShouldNotContain(own.Player.UserId);
        QueuedRecepients().ShouldContain(neighbour.Player.UserId);
    }

    [Fact]
    public async Task PlayersOfAllClaimsInRoomAreRecepients()
    {
        // Уведомление получают и сдвинутые, и те, кто остался в комнате.
        var changed = AddClaim("Вселился", playerId: 1);
        var remaining = AddClaim("Уже жил", playerId: 3);

        await CreateService().SendNotification(Notification([changed.ClaimId], [remaining.ClaimId]));

        QueuedRecepients().ShouldBe([changed.Player.UserId, remaining.Player.UserId], ignoreOrder: true);
    }

    [Fact]
    public async Task ResponsibleMasterIsRecepient()
    {
        // Ответственный мастер заявки получает уведомление независимо от своих подписок.
        var responsibleMaster = mock.CreateMaster();
        mock.ReInitProjectInfo();
        var claim = AddClaim("Вселился", playerId: 1, responsibleMaster);

        await CreateService().SendNotification(Notification([claim.ClaimId], []));

        QueuedRecepients().ShouldContain(new UserIdentification(responsibleMaster.UserId));
    }

    [Fact]
    public async Task PlayerNamesFollowOrderOfPassedClaims()
    {
        // Репозиторий отдаёт заявки в обратном порядке (см. FakeClaimsRepository), но в
        // тексте имена должны идти в порядке Changed, затем Remaining.
        var first = AddClaim("Первый", playerId: 1);
        var second = AddClaim("Второй", playerId: 3);
        var third = AddClaim("Третий", playerId: 4);

        await CreateService().SendNotification(
            Notification([first.ClaimId, second.ClaimId], [third.ClaimId]));

        var body = Queued().TemplateText.TemplateContents;
        body.IndexOf("Игрок1", StringComparison.Ordinal)
            .ShouldBeLessThan(body.IndexOf("Игрок3", StringComparison.Ordinal));
        body.IndexOf("Игрок3", StringComparison.Ordinal)
            .ShouldBeLessThan(body.IndexOf("Игрок4", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ClaimMissingFromRepositoryIsSkipped()
    {
        // Заявка удалена (или просто не попала в выборку) — уведомление об этом не падает.
        var known = AddClaim("Известный", playerId: 1);
        var unknown = new ClaimIdentification(mock.ProjectInfo.ProjectId, 9999);

        await CreateService().SendNotification(Notification([known.ClaimId, unknown], []));

        Queued().TemplateText.TemplateContents.ShouldContain("Игрок1");
        QueuedRecepients().ShouldBe([known.Player.UserId]);
    }

    [Fact]
    public async Task NoClaimsFound_NothingQueued()
    {
        // Комната без заявок: писать некому и не о ком, но и падать нельзя — уведомление
        // ставится уже ПОСЛЕ сохранения операции.
        var orphan = new ClaimIdentification(mock.ProjectInfo.ProjectId, 9999);

        await CreateService().SendNotification(Notification([orphan], []));

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task NotificationEventIsFilledForAccommodation()
    {
        var claim = AddClaim("Вселился", playerId: 1);
        var model = Notification([claim.ClaimId], []);

        await CreateService().SendNotification(model);

        var notification = Queued();
        notification.NotificationClass.ShouldBe(NotificationClass.Accommodation);
        notification.EntityReference.ShouldBe(model.RoomId);
        notification.Initiator.ShouldBe(initiator.UserId);
        notification.Header.ShouldBe("Mocked project: комната Домик 101");
    }

    [Fact]
    public async Task AccommodationTypeNameTakenFromProjectMetadata()
    {
        // Название типа проживания в тексте — из метаданных проекта, а не из модели уведомления.
        var otherType = mock.CreateAccommodationType("Люкс");
        mock.ReInitProjectInfo();
        var claim = AddClaim("Вселился", playerId: 1);

        await CreateService().SendNotification(
            Notification([claim.ClaimId], []) with { AccommodationTypeId = otherType.GetId() });

        Queued().Header.ShouldContain("Люкс");
        Queued().TemplateText.TemplateContents.ShouldContain("Люкс");
    }

    [Fact]
    public async Task SubscribedMasterIsRecepientOnlyWhenSubscribedToAccommodation()
    {
        // Подписка мастера фильтруется по признаку AccommodationChange.
        var subscribedMaster = mock.CreateMaster("Подписанный");
        var indifferentMaster = mock.CreateMaster("Безразличный");
        mock.ReInitProjectInfo();
        subscribeRepository.ForCharacters.Add(new UserSubscribe(
            subscribedMaster.ToUserInfoHeader(),
            SubscriptionOptions.CreateNoneSet() with { AccommodationChange = true }));
        subscribeRepository.ForCharacters.Add(new UserSubscribe(
            indifferentMaster.ToUserInfoHeader(),
            SubscriptionOptions.CreateNoneSet() with { Comments = true }));
        var claim = AddClaim("Вселился", playerId: 1);

        await CreateService().SendNotification(Notification([claim.ClaimId], []));

        QueuedRecepients().ShouldContain(new UserIdentification(subscribedMaster.UserId));
        QueuedRecepients().ShouldNotContain(new UserIdentification(indifferentMaster.UserId));
    }
}
