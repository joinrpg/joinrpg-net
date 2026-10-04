using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Характеризующие тесты контура приглашений к совместному проживанию: что
/// <c>AccommodationInviteServiceImpl</c> делает СЕГОДНЯ — вместе с его дефектами.
/// </summary>
/// <remarks>
/// Тесты написаны перед переводом сервиса на <c>ICharacterPropsService</c> (ADR014) и нужны
/// затем, чтобы диффом было видно: поведение сохранилось либо изменилось осознанно. Там, где
/// зафиксирован дефект, стоит комментарий, что с ним станет после миграции.
/// Уведомления проверяет соседний <see cref="AccommodationInviteNotificationTest"/>.
/// </remarks>
public class AccommodationInviteBehaviorTest : AccommodationInviteTestBase
{
    /// <summary>Стороннее приглашение: нужен кто-то, кто приглашает или приглашается ещё раз.</summary>
    private Claim CreateOutsider(string name) => CreateClaimWithAccommodation(name);

    /// <summary>
    /// Пользователь, у которого нет никакого отношения к проекту: ни игрок заявки, ни мастер.
    /// Моку он неизвестен намеренно.
    /// </summary>
    private const int StrangerUserId = 12345;

    #region CreateAccommodationInvite

    /// <summary>
    /// Пункт 1: приглашение конкретной заявки создаёт ровно одно неотвеченное приглашение
    /// с правильными сторонами.
    /// </summary>
    [Fact]
    public async Task CreateInviteToClaim_CreatesSingleUnansweredInvite()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");

        await CreateService().CreateAccommodationInvite(
            sender.GetId(),
            RequestId(sender),
            AccommodationTargetIdentification.From(receiver.GetId()));

        var invite = mock.AccommodationInvites.ShouldHaveSingleItem();
        invite.IsAccepted.ShouldBe(InviteState.Unanswered);
        invite.FromClaimId.ShouldBe(sender.ClaimId);
        invite.ToClaimId.ShouldBe(receiver.ClaimId);
        invite.ProjectId.ShouldBe(mock.Project.ProjectId);
    }

    /// <summary>
    /// Пункт 2: приглашение заявки на проживание (группы соседей) создаёт по приглашению
    /// на каждого участника группы — приглашения всегда адресованы заявке, а не группе.
    /// </summary>
    [Fact]
    public async Task CreateInviteToAccommodationRequest_CreatesInvitePerGroupMember()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var firstNeighbour = CreateClaim("Сосед1");
        var secondNeighbour = CreateClaim("Сосед2");
        var group = mock.CreateAccommodationRequest(accommodationType, firstNeighbour, secondNeighbour);

        await CreateService().CreateAccommodationInvite(
            sender.GetId(),
            RequestId(sender),
            AccommodationTargetIdentification.From(RequestId(group)));

        mock.AccommodationInvites.Count.ShouldBe(2);
        mock.AccommodationInvites.Select(invite => invite.ToClaimId)
            .ShouldBe([firstNeighbour.ClaimId, secondNeighbour.ClaimId], ignoreOrder: true);
        mock.AccommodationInvites.ShouldAllBe(invite => invite.FromClaimId == sender.ClaimId);
        mock.AccommodationInvites.ShouldAllBe(invite => invite.IsAccepted == InviteState.Unanswered);
    }

    /// <summary>
    /// Пункт 3: приглашать может только игрок самой заявки или мастер с правом расселять.
    /// Посторонний получает отказ, и в базу ничего не попадает.
    /// </summary>
    [Fact]
    public async Task CreateInvite_ByStranger_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(StrangerUserId).CreateAccommodationInvite(
                sender.GetId(),
                RequestId(sender),
                AccommodationTargetIdentification.From(receiver.GetId())));

        mock.AccommodationInvites.ShouldBeEmpty();
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
        notificationService.Invites.ShouldBeEmpty();
    }

    /// <summary>
    /// Пункт 4: успешное создание приглашения — одно сохранение, и для одиночной заявки,
    /// и для группы соседей.
    /// </summary>
    [Fact]
    public async Task CreateInviteToClaim_SavesOnce()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");

        await CreateService().CreateAccommodationInvite(
            sender.GetId(),
            RequestId(sender),
            AccommodationTargetIdentification.From(receiver.GetId()));

        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <inheritdoc cref="CreateInviteToClaim_SavesOnce"/>
    [Fact]
    public async Task CreateInviteToAccommodationRequest_SavesOnce()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var group = mock.CreateAccommodationRequest(
            accommodationType,
            CreateClaim("Сосед1"),
            CreateClaim("Сосед2"));

        await CreateService().CreateAccommodationInvite(
            sender.GetId(),
            RequestId(sender),
            AccommodationTargetIdentification.From(RequestId(group)));

        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Цель приглашения не выбрана вовсе: идентификатор не разворачивается ни в заявку, ни в группу.
    /// </summary>
    [Fact]
    public async Task CreateInvite_WithoutTarget_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().CreateAccommodationInvite(
                sender.GetId(),
                RequestId(sender),
                new AccommodationTargetIdentification(mock.ProjectInfo.ProjectId, 0)));

        exception.Message.ShouldContain("кого приглашать");
        mock.AccommodationInvites.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Заявки приглашаемого в этом проекте нет. Идентификатор цели приходит из запроса, а
    /// <c>EnsureProject</c> сверяет лишь объявленный в нём проект — поэтому существование заявки
    /// проверяется загрузчиком, и промах по идентификатору становится ошибкой, а не «игрок без
    /// типа проживания».
    /// </summary>
    [Fact]
    public async Task CreateInvite_ToUnknownClaim_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var unknownClaimId = new ClaimIdentification(mock.ProjectInfo.ProjectId, 100500);

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().CreateAccommodationInvite(
                sender.GetId(),
                RequestId(sender),
                AccommodationTargetIdentification.From(unknownClaimId)));

        mock.AccommodationInvites.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
        notificationService.Invites.ShouldBeEmpty();
    }

    /// <summary>
    /// У приглашающего не выбран тип проживания — отказ именно об этом, а не о чужой заявке на
    /// проживание: сверять <paramref name="senderRequestId"/> не с чем, и проверка пропускается.
    /// </summary>
    [Fact]
    public async Task CreateInvite_BySenderWithoutAccommodationType_ThrowsAboutMissingType()
    {
        var sender = CreateClaim("Приглашающий без типа проживания");
        var receiver = CreateClaim("Приглашаемый");
        var someoneElsesGroup = mock.CreateAccommodationRequest(accommodationType, CreateClaim("Чужая комната"));

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().CreateAccommodationInvite(
                sender.GetId(),
                RequestId(someoneElsesGroup),
                AccommodationTargetIdentification.From(receiver.GetId())));

        exception.Message.ShouldContain("не выбран тип проживания");
        mock.AccommodationInvites.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Вместимость проверяется сквозь сервис, а не только в статическом правиле: тип проживания
    /// приходит из метаданных проекта (ADR015), и это единственный тест, который видит всю связку.
    /// </summary>
    [Fact]
    public async Task CreateInvite_IntoFullRoom_ThrowsAboutSpace()
    {
        var smallType = mock.CreateAccommodationType("Двушка", capacity: 2);
        mock.ReInitProjectInfo();

        var sender = CreateClaim("Приглашающий");
        _ = mock.CreateAccommodationRequest(smallType, sender);
        var group = mock.CreateAccommodationRequest(smallType, CreateClaim("Сосед1"), CreateClaim("Сосед2"));

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().CreateAccommodationInvite(
                sender.GetId(),
                RequestId(sender),
                AccommodationTargetIdentification.From(RequestId(group))));

        exception.Message.ShouldContain("не хватает мест");
        mock.AccommodationInvites.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Приглашение — подготовка к игре, поэтому в архивном проекте оно запрещено
    /// (<c>ProjectActiveRequirement.MustBeActive</c>). Проверки активности в сервисе раньше не
    /// было вовсе — это изменение поведения, принятое вместе с миграцией.
    /// </summary>
    [Fact]
    public async Task CreateInvite_InArchivedProject_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var senderRequestId = RequestId(sender);
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().CreateAccommodationInvite(
                sender.GetId(),
                senderRequestId,
                AccommodationTargetIdentification.From(receiver.GetId())));

        mock.AccommodationInvites.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Заявка на проживание приглашающего приходит из формы отдельным идентификатором, и раньше
    /// он проверялся только на принадлежность проекту: мастер мог пригласить соседа в чужую
    /// комнату. Теперь группа берётся из самой заявки, а подсунутый чужой идентификатор — отказ.
    /// </summary>
    [Fact]
    public async Task CreateInvite_WithForeignSenderRequest_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var foreignGroup = mock.CreateAccommodationRequest(accommodationType, CreateClaim("Чужая комната"));

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().CreateAccommodationInvite(
                sender.GetId(),
                RequestId(foreignGroup),
                AccommodationTargetIdentification.From(receiver.GetId())));

        exception.Message.ShouldContain("не принадлежит");
        mock.AccommodationInvites.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Приглашаемой группы с таким идентификатором нет. Раньше операция тихо не создавала ни
    /// одного приглашения, но сохраняла изменения и отправляла уведомление в пустоту; теперь это
    /// внятный отказ.
    /// </summary>
    [Fact]
    public async Task CreateInvite_ToUnknownGroup_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var unknownGroupId = new AccommodationRequestIdentification(mock.ProjectInfo.ProjectId, 100500);

        _ = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().CreateAccommodationInvite(
                sender.GetId(),
                RequestId(sender),
                AccommodationTargetIdentification.From(unknownGroupId)));

        mock.AccommodationInvites.ShouldBeEmpty();
        SaveChangesCallCount.ShouldBe(0);
        notificationService.Invites.ShouldBeEmpty();
    }

    #endregion

    #region AcceptAccommodationInvite

    /// <summary>
    /// Пункт 5: приняв приглашение, вся принимающая сторона переезжает в заявку на проживание
    /// приглашающего.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_MovesSubjectsToInvitingGroup()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var neighbour = CreateClaim("Сосед приглашаемого");
        _ = mock.CreateAccommodationRequest(accommodationType, receiver, neighbour);
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        _ = await CreateService().AcceptAccommodationInvite(InviteId(invite));

        sender.AccommodationRequest!.Subjects
            .Select(claim => claim.ClaimId)
            .ShouldBe([sender.ClaimId, receiver.ClaimId, neighbour.ClaimId], ignoreOrder: true);
        receiver.AccommodationRequest.ShouldBe(sender.AccommodationRequest);
    }

    /// <summary>
    /// Пункт 6: опустевшая заявка на проживание принимающей стороны удаляется — иначе в базе
    /// осталась бы группа без жильцов.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_RemovesEmptiedReceiverRequest()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var receiverRequest = mock.CreateAccommodationRequest(accommodationType, receiver);
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        _ = await CreateService().AcceptAccommodationInvite(InviteId(invite));

        mock.AccommodationRequests.ShouldNotContain(receiverRequest);
        mock.AccommodationRequests.ShouldHaveSingleItem().ShouldBe(sender.AccommodationRequest);
    }

    /// <summary>Пункт 7: само принятое приглашение помечается принятым.</summary>
    [Fact]
    public async Task AcceptInvite_MarksInviteAccepted()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        var result = await CreateService().AcceptAccommodationInvite(InviteId(invite));

        result.ShouldBe(invite);
        invite.IsAccepted.ShouldBe(InviteState.Accepted);
        invite.ResolveDescription.ShouldBe(ResolveDescription.Accepted);
    }

    /// <summary>
    /// Пункт 8: приватный <c>DeclineOtherInvite</c> снимает только ВХОДЯЩИЕ приглашения
    /// (фильтр только по <c>ToClaimId</c>) и только у тех, кто переезжает вместе с принявшим,
    /// — то есть у соседей принявшего, но не у него самого: его заявку убирают из группы
    /// до цикла (<c>Subjects.Remove</c>), и в цикл она уже не попадает.
    /// </summary>
    /// <remarks>
    /// Сейчас так; после миграции на ADR014 снятие прочих приглашений станет общим для всех
    /// переезжающих заявок, принявшую включая, и тест обновится вместе с ней.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_DeclinesIncomingInvitesOfMovedNeighboursOnly()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var neighbour = CreateClaim("Сосед приглашаемого");
        _ = mock.CreateAccommodationRequest(accommodationType, receiver, neighbour);
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        var outsider = CreateOutsider("Сторонний приглашающий");
        var outsiderTarget = CreateClaim("Сторонний приглашаемый");
        // Входящие приглашения: принявшему и его соседу
        var incomingToReceiver = mock.CreateAccommodationInvite(outsider, receiver);
        var incomingToNeighbour = mock.CreateAccommodationInvite(outsider, neighbour);
        // Исходящее приглашение соседа
        var outgoingFromNeighbour = mock.CreateAccommodationInvite(neighbour, outsiderTarget);

        _ = await CreateService().AcceptAccommodationInvite(InviteId(invite));

        // Сосед переехал вместе с принявшим — его входящее приглашение снято
        incomingToNeighbour.IsAccepted.ShouldBe(InviteState.Declined);
        incomingToNeighbour.ResolveDescription.ShouldBe(ResolveDescription.DeclinedWithAcceptOther);

        // Исходящие не трогаются вообще: фильтр только по ToClaimId
        outgoingFromNeighbour.IsAccepted.ShouldBe(InviteState.Unanswered);
        outgoingFromNeighbour.ResolveDescription.ShouldBe(ResolveDescription.Unspecified);

        // А вот входящее приглашение самого принявшего остаётся висеть неотвеченным — дефект:
        // игрок уже переехал к приглашающему, но может принять и другое приглашение.
        incomingToReceiver.IsAccepted.ShouldBe(InviteState.Unanswered);
    }

    /// <summary>
    /// Пункт 9: мест на всех приглашённых не хватает — операция возвращает <c>null</c>
    /// и ничего не меняет. Отказ при этом молчаливый (в коде так и помечено <c>todo</c>):
    /// игрок не узнает причину.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_NoFreeSpace_ReturnsNullAndChangesNothing()
    {
        var smallType = mock.CreateAccommodationType("Двушка", capacity: 2);
        mock.ReInitProjectInfo();

        var sender = CreateClaim("Приглашающий");
        var senderNeighbour = CreateClaim("Сосед приглашающего");
        var senderRequest = mock.CreateAccommodationRequest(smallType, sender, senderNeighbour);

        var receiver = CreateClaim("Приглашаемый");
        var receiverRequest = mock.CreateAccommodationRequest(smallType, receiver);
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        var result = await CreateService().AcceptAccommodationInvite(InviteId(invite));

        result.ShouldBeNull();
        invite.IsAccepted.ShouldBe(InviteState.Unanswered);
        invite.ResolveDescription.ShouldBe(ResolveDescription.Unspecified);
        senderRequest.Subjects.Select(claim => claim.ClaimId)
            .ShouldBe([sender.ClaimId, senderNeighbour.ClaimId], ignoreOrder: true);
        receiverRequest.Subjects.ShouldHaveSingleItem().ShouldBe(receiver);
        mock.AccommodationRequests.ShouldContain(receiverRequest);
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
        notificationService.Invites.ShouldBeEmpty();
    }

    /// <summary>
    /// Пункт 10: принять приглашение сегодня может ЛЮБОЙ пользователь — проверки прав в
    /// <c>AcceptAccommodationInvite</c> нет вообще, достаточно знать идентификатор приглашения.
    /// </summary>
    /// <remarks>
    /// IDOR, закрывается в PR 2 стека: там появится <c>ClaimAccessRequirement.AccommodationChange</c>
    /// и тест превратится в проверку отказа.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_ByUnrelatedUser_StillAccepts()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        var result = await CreateService(StrangerUserId).AcceptAccommodationInvite(InviteId(invite));

        result.ShouldBe(invite);
        invite.IsAccepted.ShouldBe(InviteState.Accepted);
    }

    /// <summary>
    /// Пункт 11: сохранений на успешном приёме несколько — приватный <c>DeclineOtherInvite</c>
    /// сохраняет на каждого переезжающего соседа, и ещё одно сохранение идёт в конце.
    /// </summary>
    /// <remarks>
    /// Сейчас так (N+1 по числу переезжающих соседей); после миграции на ADR014 сохранение
    /// станет одно, и тест обновится вместе с ней.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_SavesOncePerMovedNeighbourPlusOne()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var firstNeighbour = CreateClaim("Сосед1");
        var secondNeighbour = CreateClaim("Сосед2");
        _ = mock.CreateAccommodationRequest(accommodationType, receiver, firstNeighbour, secondNeighbour);
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        _ = await CreateService().AcceptAccommodationInvite(InviteId(invite));

        unitOfWork.SaveChangesCallCount.ShouldBe(3);
    }

    /// <summary>
    /// Для сравнения с предыдущим: приглашённый без своей заявки на проживание переезжает
    /// за одно сохранение — цикла по соседям просто нет.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_ReceiverWithoutOwnRequest_SavesOnce()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        _ = await CreateService().AcceptAccommodationInvite(InviteId(invite));

        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    #endregion

    #region CancelOrDeclineAccommodationInvite

    /// <summary>Пункт 12: отклонение приглашения приглашённым.</summary>
    [Fact]
    public async Task DeclineInvite_MarksInviteDeclined()
    {
        var invite = CreateInviteBetweenNewClaims();

        var result = await CreateService().CancelOrDeclineAccommodationInvite(
            InviteId(invite), InviteState.Declined);

        result.ShouldBe(invite);
        invite.IsAccepted.ShouldBe(InviteState.Declined);
        invite.ResolveDescription.ShouldBe(ResolveDescription.Declined);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>Пункт 12: отзыв приглашения приглашающим.</summary>
    [Fact]
    public async Task CancelInvite_MarksInviteCanceled()
    {
        var invite = CreateInviteBetweenNewClaims();

        var result = await CreateService().CancelOrDeclineAccommodationInvite(
            InviteId(invite), InviteState.Canceled);

        result.ShouldBe(invite);
        invite.IsAccepted.ShouldBe(InviteState.Canceled);
        invite.ResolveDescription.ShouldBe(ResolveDescription.Canceled);
        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Пункт 13: операция умеет только отклонить и отозвать. Прочие состояния — молчаливый
    /// <c>null</c>, без изменений и без сохранения.
    /// </summary>
    [Theory]
    [InlineData(InviteState.Unanswered)]
    [InlineData(InviteState.Accepted)]
    public async Task CancelOrDeclineInvite_UnsupportedState_ReturnsNull(InviteState newState)
    {
        var invite = CreateInviteBetweenNewClaims();

        var result = await CreateService().CancelOrDeclineAccommodationInvite(InviteId(invite), newState);

        result.ShouldBeNull();
        invite.IsAccepted.ShouldBe(InviteState.Unanswered);
        invite.ResolveDescription.ShouldBe(ResolveDescription.Unspecified);
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
        notificationService.Invites.ShouldBeEmpty();
    }

    /// <summary>
    /// Пункт 14: приглашения с таким идентификатором нет — летит нетипизированное
    /// <see cref="Exception"/> с английским текстом.
    /// </summary>
    /// <remarks>
    /// Сейчас так; после миграции на ADR014 это станет внятной ошибкой домена, и тест
    /// обновится вместе с ней. В <c>AcceptAccommodationInvite</c> тот же промах по
    /// идентификатору даёт <see cref="NullReferenceException"/> — см. соседний тест.
    /// </remarks>
    [Fact]
    public async Task CancelOrDeclineInvite_UnknownInvite_Throws()
    {
        var unknownInviteId = new AccommodationInviteIdentification(mock.ProjectInfo.ProjectId, 100500);

        var exception = await Should.ThrowAsync<Exception>(
            () => CreateService().CancelOrDeclineAccommodationInvite(unknownInviteId, InviteState.Declined));

        exception.Message.ShouldBe("Invite request not found.");
        unitOfWork.SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Промах по идентификатору в приёме приглашения не проверяется вообще и даёт NRE —
    /// пользователь видит «что-то пошло не так». Сейчас так; после миграции на ADR014
    /// станет внятной ошибкой домена, и тест обновится вместе с ней.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_UnknownInvite_ThrowsNullReference()
    {
        var unknownInviteId = new AccommodationInviteIdentification(mock.ProjectInfo.ProjectId, 100500);

        _ = await Should.ThrowAsync<NullReferenceException>(
            () => CreateService().AcceptAccommodationInvite(unknownInviteId));
    }

    /// <summary>
    /// Пункт 15: отклонить или отозвать приглашение сегодня может ЛЮБОЙ пользователь —
    /// проверки прав в <c>CancelOrDeclineAccommodationInvite</c> нет.
    /// </summary>
    /// <remarks>
    /// IDOR, закрывается в PR 3 стека — там операция разделится на отклонение и отзыв, у каждого
    /// появится своё <c>ClaimAccessRequirement.AccommodationChange</c>, и тест превратится
    /// в проверку отказа.
    /// </remarks>
    [Fact]
    public async Task CancelOrDeclineInvite_ByUnrelatedUser_StillChangesInvite()
    {
        var invite = CreateInviteBetweenNewClaims();

        var result = await CreateService(StrangerUserId).CancelOrDeclineAccommodationInvite(
            InviteId(invite), InviteState.Canceled);

        result.ShouldBe(invite);
        invite.IsAccepted.ShouldBe(InviteState.Canceled);
    }

    #endregion

    private AccommodationInvite CreateInviteBetweenNewClaims()
        => mock.CreateAccommodationInvite(
            CreateClaimWithAccommodation("Приглашающий"),
            CreateClaim("Приглашаемый"));
}
