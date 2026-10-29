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
            Player = new UserInfoHeader(new UserIdentification(playerId), new UserDisplayName($"Игрок{playerId}", null), Avatar: null),
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
            mock.ProjectInfo.AccommodationSettings.GetTypeById(accommodationType.GetId()).RoomCategoryId,
            initiatorOverride ?? initiator,
            changed,
            remaining,
            RoomOccupancyChangeKind.Occupied);

    /// <param name="host">Заявка, на странице которой выполнили операцию; по умолчанию — первая из
    /// <paramref name="recipientClaims"/>.</param>
    private AccommodationInviteNotification Invite(
        IReadOnlyCollection<ClaimIdentification> recipientClaims,
        UserInfoHeader? initiatorOverride = null,
        InviteChangeKind kind = InviteChangeKind.Created,
        ClaimIdentification? host = null)
        => new(
            host ?? recipientClaims.FirstOrDefault() ?? new ClaimIdentification(mock.ProjectInfo.ProjectId, 9999),
            recipientClaims,
            initiatorOverride ?? initiator,
            kind);

    private NotificationEvent Queued() => notificationService.Queued.Single();

    private IReadOnlyCollection<UserIdentification> QueuedRecepients()
        => [.. Queued().Recepients.Select(r => r.UserId)];

    /// <summary>
    /// Уведомление, поставленное по конкретной заявке: приглашения ставятся по одному на заявку,
    /// и найти своё можно только по ссылке на неё.
    /// </summary>
    private NotificationEvent QueuedFor(ClaimIdentification claimId)
        => notificationService.Queued.Single(notification => Equals(notification.EntityReference, claimId));

    private IReadOnlyCollection<UserIdentification> RecepientsOf(ClaimIdentification claimId)
        => [.. QueuedFor(claimId).Recepients.Select(r => r.UserId)];

    /// <summary>Получатели всех поставленных уведомлений вместе.</summary>
    private List<UserIdentification> AllRecepients()
        => [.. notificationService.Queued.SelectMany(n => n.Recepients).Select(r => r.UserId)];

    /// <summary>Сколько уведомлений получил пользователь.</summary>
    private int QueuedCountFor(User user)
        => notificationService.Queued.Count(
            notification => notification.Recepients.Any(r => r.UserId == new UserIdentification(user.UserId)));

    /// <summary>Поставлено ли вообще уведомление по этой заявке.</summary>
    private bool QueuedAnythingFor(ClaimIdentification claimId)
        => notificationService.Queued.Any(notification => Equals(notification.EntityReference, claimId));

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
            Notification([claim.ClaimId], []) with
            {
                RoomCategoryId = mock.ProjectInfo.AccommodationSettings.GetTypeById(otherType.GetId()).RoomCategoryId,
            });

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

    [Fact]
    public async Task Invite_OneNotificationPerClaimWithLinkToThatClaim()
    {
        // Уведомление ставится по одному на каждую заявку, и ссылка в нём ведёт именно на эту
        // заявку: приглашениями управляют на странице заявки получателя, а она у каждого своя.
        var first = AddClaim("Первый", playerId: 1);
        var second = AddClaim("Второй", playerId: 3);

        await CreateService().SendNotification(Invite([first.ClaimId, second.ClaimId]));

        notificationService.Queued.Count.ShouldBe(2);
        notificationService.Queued.Select(notification => notification.EntityReference)
            .ShouldBe([first.ClaimId, second.ClaimId], ignoreOrder: true);
    }

    [Fact]
    public async Task Invite_RecepientsAreCountedPerClaim()
    {
        // Получатели каждого уведомления считаются от своей заявки: игрок другой заявки из того же
        // вызова в них не попадает, хотя заявки пришли одним списком.
        var first = AddClaim("Первый", playerId: 1);
        var second = AddClaim("Второй", playerId: 3);

        await CreateService().SendNotification(Invite([first.ClaimId, second.ClaimId]));

        RecepientsOf(first.ClaimId).ShouldBe([first.Player.UserId]);
        RecepientsOf(second.ClaimId).ShouldBe([second.Player.UserId]);
    }

    [Fact]
    public async Task Invite_ResponsibleMasterIsNotRecepient()
    {
        // Ответственный мастер на приглашения не подписан: предикат AccommodationInvitesChange, а
        // SubscribeCalculator.CreateForRespMaster выставляет его в false. Это отличие от
        // уведомлений о комнате — там мастер получает, см. ResponsibleMasterIsRecepient.
        var responsibleMaster = mock.CreateMaster();
        mock.ReInitProjectInfo();
        var claim = AddClaim("Приглашённый", playerId: 1, responsibleMaster);

        await CreateService().SendNotification(Invite([claim.ClaimId]));

        RecepientsOf(claim.ClaimId).ShouldBe([claim.Player.UserId]);
        RecepientsOf(claim.ClaimId).ShouldNotContain(new UserIdentification(responsibleMaster.UserId));
    }

    [Fact]
    public async Task Invite_InitiatorIsNotRecepient()
    {
        // Инициатор приглашения не должен получить уведомление о своём же действии, даже если он
        // игрок одной из заявок-получателей (отмена приглашения уведомляет обе стороны). По его
        // заявке уведомление не ставится вовсе: получателей у него не остаётся, а пустое
        // уведомление — это лишние запросы и вводящая в заблуждение строка в логе.
        var own = AddClaim("Свой", playerId: 1);
        var other = AddClaim("Другой", playerId: 3);

        await CreateService().SendNotification(
            Invite([own.ClaimId, other.ClaimId], initiatorOverride: own.Player));

        QueuedAnythingFor(own.ClaimId).ShouldBeFalse();
        RecepientsOf(other.ClaimId).ShouldBe([other.Player.UserId]);
    }

    /// <summary>
    /// Зеркало <see cref="SubscribedMasterIsRecepientOnlyWhenSubscribedToAccommodation"/> для
    /// приглашений: здесь предикат другой — AccommodationInvitesChange. Тест нужен в обе стороны,
    /// иначе опечатка в предикате (взяли не то поле) прошла бы незамеченной.
    /// </summary>
    [Fact]
    public async Task Invite_SubscribedMasterIsRecepientOnlyWhenSubscribedToInvites()
    {
        var subscribedMaster = mock.CreateMaster("Подписанный на приглашения");
        var roomOnlyMaster = mock.CreateMaster("Подписанный только на комнаты");
        mock.ReInitProjectInfo();
        subscribeRepository.ForCharacters.Add(new UserSubscribe(
            subscribedMaster.ToUserInfoHeader(),
            SubscriptionOptions.CreateNoneSet() with { AccommodationInvitesChange = true }));
        subscribeRepository.ForCharacters.Add(new UserSubscribe(
            roomOnlyMaster.ToUserInfoHeader(),
            SubscriptionOptions.CreateNoneSet() with { AccommodationChange = true }));
        var claim = AddClaim("Приглашённый", playerId: 1);

        await CreateService().SendNotification(Invite([claim.ClaimId]));

        var recipients = AllRecepients();
        recipients.ShouldContain(new UserIdentification(subscribedMaster.UserId));
        recipients.ShouldNotContain(new UserIdentification(roomOnlyMaster.UserId));
    }

    [Fact]
    public async Task Invite_NotificationEventIsFilledForAccommodation()
    {
        var claim = AddClaim("Приглашённый", playerId: 1);

        await CreateService().SendNotification(Invite([claim.ClaimId]));

        var notification = Queued();
        notification.NotificationClass.ShouldBe(NotificationClass.Accommodation);
        notification.Initiator.ShouldBe(initiator.UserId);
        notification.Header.ShouldContain(mock.ProjectInfo.ProjectName.Value);
        notification.Header.ShouldContain("приглашения к проживанию");
    }

    [Fact]
    public async Task Invite_NoClaims_NothingQueued()
    {
        // Пустой список получателей и заявка, которой нет в выборке: уведомление ставится уже
        // ПОСЛЕ сохранения операции, поэтому падать здесь нельзя.
        var orphan = new ClaimIdentification(mock.ProjectInfo.ProjectId, 9999);

        await CreateService().SendNotification(Invite([]));
        await CreateService().SendNotification(Invite([orphan]));

        notificationService.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task Invite_MasterOfSeveralClaims_GetsSingleNotificationLinkedToHost()
    {
        // Приглашение целой группы: мастер, подписанный на все её заявки, раньше получал по письму
        // на каждую (#5676). Теперь одно, со ссылкой на заявку, на странице которой приглашали.
        var otherMaster = mock.CreateMaster();
        mock.ReInitProjectInfo();
        SubscribeToInvites(otherMaster);
        var sender = AddClaim("Приглашающий", playerId: 1);
        var first = AddClaim("Первый", playerId: 3);
        var second = AddClaim("Второй", playerId: 4);

        await CreateService().SendNotification(
            Invite([first.ClaimId, second.ClaimId], host: sender.ClaimId));

        var masterId = new UserIdentification(otherMaster.UserId);
        var toMaster = notificationService.Queued
            .Where(notification => notification.Recepients.Any(r => r.UserId == masterId))
            .ShouldHaveSingleItem();
        toMaster.EntityReference.ShouldBe(sender.ClaimId);
        toMaster.Recepients.ShouldHaveSingleItem();

        // Игрокам — по-прежнему каждому своё и без мастера.
        RecepientsOf(first.ClaimId).ShouldBe([first.Player.UserId]);
        RecepientsOf(second.ClaimId).ShouldBe([second.Player.UserId]);
    }

    [Fact]
    public async Task Invite_PlayerWhoIsAlsoSubscribedMaster_GetsOnlyPlayerNotification()
    {
        // Отмена уведомляет обе стороны: игрок одной из них, подписанный как мастер на другую,
        // получает одно уведомление — своё, со ссылкой на свою заявку.
        var playingMaster = mock.CreateMaster();
        mock.ReInitProjectInfo();
        SubscribeToInvites(playingMaster);
        var own = AddClaim("Свой", playerId: playingMaster.UserId);
        var other = AddClaim("Другой", playerId: 3);

        await CreateService().SendNotification(
            Invite([own.ClaimId, other.ClaimId], kind: InviteChangeKind.Cancelled, host: other.ClaimId));

        QueuedCountFor(playingMaster).ShouldBe(1);
        RecepientsOf(own.ClaimId).ShouldBe([new UserIdentification(playingMaster.UserId)]);
    }

    private void SubscribeToInvites(User master)
        => subscribeRepository.ForCharacters.Add(new UserSubscribe(
            master.ToUserInfoHeader(),
            SubscriptionOptions.CreateNoneSet() with { AccommodationInvitesChange = true }));

    /// <summary>
    /// Подписка — как у приглашений: объединение их заменяет. Ответственный мастер на приглашения не
    /// подписан, подписанный только на изменения проживания — тоже не получает.
    /// </summary>
    [Fact]
    public async Task Merge_GoesToInviteSubscribersOnly()
    {
        var responsibleMaster = mock.CreateMaster("Ответственный");
        var accommodationOnly = mock.CreateMaster("Только проживание");
        var invitesSubscriber = mock.CreateMaster("Приглашения");
        mock.ReInitProjectInfo();
        subscribeRepository.ForCharacters.Add(new UserSubscribe(
            accommodationOnly.ToUserInfoHeader(),
            SubscriptionOptions.CreateNoneSet() with { AccommodationChange = true }));
        SubscribeToInvites(invitesSubscriber);
        var host = AddClaim("Хозяин", playerId: 1, responsibleMaster);
        var guest = AddClaim("Гость", playerId: 3, responsibleMaster);

        await CreateService().SendNotification(Merged(host, guest));

        var recipients = AllRecepients();
        recipients.ShouldContain(new UserIdentification(invitesSubscriber.UserId));
        recipients.ShouldNotContain(new UserIdentification(responsibleMaster.UserId));
        recipients.ShouldNotContain(new UserIdentification(accommodationOnly.UserId));
    }

    private AccommodationGroupMergedNotification Merged(ClaimWithPlayer host, params ClaimWithPlayer[] others)
        => new(host.ClaimId, [host.ClaimId, .. others.Select(claim => claim.ClaimId)], initiator);

    [Fact]
    public async Task Merge_EachPlayerGetsLinkToOwnClaim()
    {
        // Инициатор — mock.Master, он же ответственный мастер обеих заявок: себе он не пишет, так
        // что мастерского уведомления нет вовсе.
        var host = AddClaim("Хозяин", playerId: 1);
        var guest = AddClaim("Гость", playerId: 3);

        await CreateService().SendNotification(Merged(host, guest));

        notificationService.Queued.Count.ShouldBe(2);
        RecepientsOf(host.ClaimId).ShouldBe([host.Player.UserId]);
        RecepientsOf(guest.ClaimId).ShouldBe([guest.Player.UserId]);
    }

    [Fact]
    public async Task Merge_MasterOfSeveralClaims_GetsSingleNotificationLinkedToHost()
    {
        // Мастер, подписанный на все заявки группы, получил бы по письму на каждую — теперь одно,
        // со ссылкой на заявку, на странице которой объединяли.
        var otherMaster = mock.CreateMaster();
        mock.ReInitProjectInfo();
        SubscribeToInvites(otherMaster);
        var host = AddClaim("Хозяин", playerId: 1);
        var guest = AddClaim("Гость", playerId: 3);
        var third = AddClaim("Третий", playerId: 4);

        await CreateService().SendNotification(Merged(host, guest, third));

        var masterId = new UserIdentification(otherMaster.UserId);
        var toMaster = notificationService.Queued
            .Where(notification => notification.Recepients.Any(r => r.UserId == masterId))
            .ShouldHaveSingleItem();
        toMaster.EntityReference.ShouldBe(host.ClaimId);
        toMaster.Recepients.ShouldHaveSingleItem();

        // Игрокам — по-прежнему каждому своё и без мастера.
        RecepientsOf(guest.ClaimId).ShouldBe([guest.Player.UserId]);
        RecepientsOf(third.ClaimId).ShouldBe([third.Player.UserId]);
    }

    [Fact]
    public async Task Merge_PlayerWhoIsAlsoMasterOfAnotherClaim_GetsOnlyPlayerNotification()
    {
        var playingMaster = mock.CreateMaster();
        mock.ReInitProjectInfo();
        SubscribeToInvites(playingMaster);
        var host = AddClaim("Хозяин", playerId: playingMaster.UserId);
        var guest = AddClaim("Гость", playerId: 3);

        await CreateService().SendNotification(Merged(host, guest));

        QueuedCountFor(playingMaster).ShouldBe(1);
        RecepientsOf(host.ClaimId).ShouldBe([new UserIdentification(playingMaster.UserId)]);
    }
}
