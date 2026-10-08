using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Поведение контура приглашений к совместному проживанию: создание, приём, отказ и отзыв —
/// вместе с отказами по правам, по состоянию приглашения и по нехватке мест.
/// </summary>
/// <remarks>
/// Тесты заведены характеризующими — до перевода сервиса на <c>ICharacterPropsService</c>
/// (ADR014), — чтобы миграцию было видно диффом: каждый тест, который при переезде изменился,
/// описывает в <c>remarks</c>, как операция вела себя раньше. Уведомления проверяет соседний
/// <see cref="AccommodationInviteNotificationTest"/>.
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
            AccommodationGroupIdentification.From(receiver.GetId()));

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
            AccommodationGroupIdentification.From(RequestId(group)));

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
                AccommodationGroupIdentification.From(receiver.GetId())));

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
            AccommodationGroupIdentification.From(receiver.GetId()));

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
            AccommodationGroupIdentification.From(RequestId(group)));

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
                new AccommodationGroupIdentification(mock.ProjectInfo.ProjectId, 0)));

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
                AccommodationGroupIdentification.From(unknownClaimId)));

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
                AccommodationGroupIdentification.From(receiver.GetId())));

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
                AccommodationGroupIdentification.From(RequestId(group))));

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
                AccommodationGroupIdentification.From(receiver.GetId())));

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
                AccommodationGroupIdentification.From(receiver.GetId())));

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
                AccommodationGroupIdentification.From(unknownGroupId)));

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

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

        sender.AccommodationRequest!.Subjects
            .Select(claim => claim.ClaimId)
            .ShouldBe([sender.ClaimId, receiver.ClaimId, neighbour.ClaimId], ignoreOrder: true);

        // Обе стороны связи проставлены всем переезжающим, а не только принявшей заявке: до
        // миграции соседи полагались на relationship fixup EF6.
        foreach (var moved in new[] { receiver, neighbour })
        {
            moved.AccommodationRequest.ShouldBe(sender.AccommodationRequest);
            moved.AccommodationRequest_Id.ShouldBe(sender.AccommodationRequest!.Id);
        }
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

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

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

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

        invite.IsAccepted.ShouldBe(InviteState.Accepted);
        invite.ResolveDescription.ShouldBe(ResolveDescription.Accepted);
    }

    /// <summary>
    /// Приём снимает прочие ВХОДЯЩИЕ приглашения у всех, кто переехал, — и у соседей принявшего,
    /// и у него самого. Исходящие не трогаются: их снимает отказ или отзыв.
    /// </summary>
    /// <remarks>
    /// До миграции собственные входящие приглашения принявшего оставались висеть: его заявку
    /// убирали из группы раньше цикла снятия, и в цикл она не попадала — игрок мог принять
    /// приглашение и тут же принять второе.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_DeclinesIncomingInvitesOfEveryoneMoved()
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

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

        // Сосед переехал вместе с принявшим — его входящее приглашение снято
        incomingToNeighbour.IsAccepted.ShouldBe(InviteState.Declined);
        incomingToNeighbour.ResolveDescription.ShouldBe(ResolveDescription.DeclinedWithAcceptOther);

        // Исходящие не трогаются вообще: фильтр только по ToClaimId
        outgoingFromNeighbour.IsAccepted.ShouldBe(InviteState.Unanswered);
        outgoingFromNeighbour.ResolveDescription.ShouldBe(ResolveDescription.Unspecified);

        // Входящее приглашение самого принявшего тоже снято: он уже переехал, отвечать на него нечем
        incomingToReceiver.IsAccepted.ShouldBe(InviteState.Declined);
        incomingToReceiver.ResolveDescription.ShouldBe(ResolveDescription.DeclinedWithAcceptOther);
    }

    /// <summary>
    /// Мест на всех переезжающих не хватает — внятный отказ, и ничего не изменилось.
    /// </summary>
    /// <remarks>
    /// До миграции операция молча возвращала <c>null</c> (в коде стояло <c>todo: make null result
    /// descriptive</c>), и игрок не узнавал причину. Теперь это то же исключение, что у создания
    /// приглашения, а контроллер отдаёт его текстом.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_NoFreeSpace_ThrowsAndChangesNothing()
    {
        var smallType = mock.CreateAccommodationType("Двушка", capacity: 2);
        mock.ReInitProjectInfo();

        var sender = CreateClaim("Приглашающий");
        var senderNeighbour = CreateClaim("Сосед приглашающего");
        var senderRequest = mock.CreateAccommodationRequest(smallType, sender, senderNeighbour);

        var receiver = CreateClaim("Приглашаемый");
        var receiverRequest = mock.CreateAccommodationRequest(smallType, receiver);
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().AcceptAccommodationInvite(InviteId(invite)));

        exception.Message.ShouldContain("не хватает мест");
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
    /// Принять приглашение может только тот, кому разрешено менять проживание принимающей заявки:
    /// сам игрок, ответственный мастер или мастер с правом расселять. Посторонний получает отказ.
    /// </summary>
    /// <remarks>
    /// До миграции проверки прав здесь не было вовсе — зная идентификатор приглашения, принять его
    /// мог любой аутентифицированный пользователь и тем переселить людей по комнатам.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_ByUnrelatedUser_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(StrangerUserId).AcceptAccommodationInvite(InviteId(invite)));

        invite.IsAccepted.ShouldBe(InviteState.Unanswered);
        SaveChangesCallCount.ShouldBe(0);
        notificationService.Invites.ShouldBeEmpty();
    }

    /// <summary>
    /// Приём — одна операция и одно сохранение, сколько бы соседей ни переезжало.
    /// </summary>
    /// <remarks>
    /// До миграции приватный <c>DeclineOtherInvite</c> сохранял на каждого переезжающего соседа,
    /// то есть операция была неатомарной: падение на середине оставляло часть приглашений снятыми,
    /// а переселение несделанным.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_SavesOnce()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var firstNeighbour = CreateClaim("Сосед1");
        var secondNeighbour = CreateClaim("Сосед2");
        _ = mock.CreateAccommodationRequest(accommodationType, receiver, firstNeighbour, secondNeighbour);
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// То же и для приглашённого без своей заявки на проживание: он переезжает один.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_ReceiverWithoutOwnRequest_SavesOnce()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

        unitOfWork.SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Расселённая группа считает свободные места по своей комнате, а не по типу проживания:
    /// в комнате живут не только соседи из той же группы. Вместимость при этом приходит из
    /// метаданных проекта (ADR015), а не из навигации на тип.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_IntoFullRoom_Throws()
    {
        var smallType = mock.CreateAccommodationType("Двушка", capacity: 2);
        mock.ReInitProjectInfo();

        var sender = CreateClaim("Приглашающий");
        var senderGroup = mock.CreateAccommodationRequest(smallType, sender);
        var room = mock.CreateRoom(senderGroup, "Двушка №1");

        // В той же комнате живёт ещё одна группа — мест больше нет
        var neighbourGroup = mock.CreateAccommodationRequest(smallType, CreateClaim("Уже живёт"));
        room.Inhabitants.Add(neighbourGroup);
        neighbourGroup.Accommodation = room;
        neighbourGroup.AccommodationId = room.Id;

        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().AcceptAccommodationInvite(InviteId(invite)));

        exception.Message.ShouldContain("не хватает мест");
        receiver.AccommodationRequest.ShouldBeNull();
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Та же комнатная ветка расчёта, но с запасом мест: приём проходит, и принявший оказывается
    /// в комнате приглашающего.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_IntoRoomWithFreeSpace_Moves()
    {
        var sender = CreateClaim("Приглашающий");
        var senderGroup = mock.CreateAccommodationRequest(accommodationType, sender);
        var room = mock.CreateRoom(senderGroup, "Домик №1");

        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

        receiver.AccommodationRequest.ShouldBe(senderGroup);
        senderGroup.Accommodation.ShouldBe(room);
        room.GetAllInhabitants().Select(claim => claim.ClaimId)
            .ShouldBe([sender.ClaimId, receiver.ClaimId], ignoreOrder: true);
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Опустевшая группа принимающего удаляется и тогда, когда была расселена по комнате: иначе в
    /// комнате осталась бы группа без жильцов, занимающая место.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_RemovesEmptiedReceiverRequestSettledInRoom()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var receiverGroup = mock.CreateAccommodationRequest(accommodationType, receiver);
        _ = mock.CreateRoom(receiverGroup, "Комната приглашаемого");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

        mock.AccommodationRequests.ShouldNotContain(receiverGroup);
        receiver.AccommodationRequest.ShouldBe(sender.AccommodationRequest);
    }

    /// <summary>
    /// Принять приглашение может и сам игрок принимающей заявки, не только мастер.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_ByPlayer_Accepts()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        await CreateService(mock.Player.UserId).AcceptAccommodationInvite(InviteId(invite));

        invite.IsAccepted.ShouldBe(InviteState.Accepted);
        receiver.AccommodationRequest.ShouldBe(sender.AccommodationRequest);
    }

    /// <summary>
    /// На приглашение уже ответили — повторный ответ отклоняется. Проверки состояния раньше не
    /// было, а эндпойнт не идемпотентен: повторный приём доходил до операции, где приглашающий и
    /// принимающий уже соседи, и отправлял их общую группу на удаление вместе с жильцами.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_Twice_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);
        var senderGroup = sender.AccommodationRequest;

        await CreateService().AcceptAccommodationInvite(InviteId(invite));

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().AcceptAccommodationInvite(InviteId(invite)));

        exception.Message.ShouldContain("уже ответили");
        mock.AccommodationRequests.ShouldContain(senderGroup!);
        receiver.AccommodationRequest.ShouldBe(senderGroup);
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Встречные приглашения: A пригласил B, B пригласил A. Принято одно — второе сбылось само:
    /// стороны уже живут вместе, отвечать на него нечего, и оно уходит в терминальный статус
    /// «принято автоматически», а не остаётся висеть неотвеченным.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_ResolvesCounterpartInvite()
    {
        var first = CreateClaimWithAccommodation("Первый");
        var second = CreateClaimWithAccommodation("Второй");
        var counterpartInvite = mock.CreateAccommodationInvite(first, second);
        var acceptedInvite = mock.CreateAccommodationInvite(second, first);

        // Первый принял приглашение второго и переехал к нему
        await CreateService().AcceptAccommodationInvite(InviteId(acceptedInvite));

        acceptedInvite.IsAccepted.ShouldBe(InviteState.Accepted);
        acceptedInvite.ResolveDescription.ShouldBe(ResolveDescription.Accepted);

        counterpartInvite.IsAccepted.ShouldBe(InviteState.Accepted);
        counterpartInvite.ResolveDescription.ShouldBe(ResolveDescription.AcceptedAuto);

        first.AccommodationRequest.ShouldBe(second.AccommodationRequest);
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Приглашение между теми, кто и так живёт в одной группе. Нормальным потоком такое не
    /// создаётся (<c>EnsureCanInvite</c> не даёт приглашать своих) и не остаётся висеть — но в базе
    /// лежат строки, созданные до этих правил, и принимать их нельзя: переезжать некуда, а группа,
    /// которую приём считает опустевшей, — это та самая группа, где все и живут.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_WhenAlreadyNeighbours_Throws()
    {
        var first = CreateClaim("Первый");
        var second = CreateClaim("Второй");
        var commonGroup = mock.CreateAccommodationRequest(accommodationType, first, second);
        var legacyInvite = mock.CreateAccommodationInvite(first, second);

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().AcceptAccommodationInvite(InviteId(legacyInvite)));

        exception.Message.ShouldContain("уже живёт вместе");
        mock.AccommodationRequests.ShouldContain(commonGroup);
        commonGroup.Subjects.Select(claim => claim.ClaimId)
            .ShouldBe([first.ClaimId, second.ClaimId], ignoreOrder: true);
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// У приглашающего больше нет типа проживания — принимать приглашение некуда. Раньше здесь
    /// падал <see cref="NullReferenceException"/>: результат запроса за его группой не проверялся.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_SenderWithoutAccommodationType_Throws()
    {
        var sender = CreateClaim("Приглашающий без типа проживания");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().AcceptAccommodationInvite(InviteId(invite)));

        exception.Message.ShouldContain("не выбран тип проживания");
        invite.IsAccepted.ShouldBe(InviteState.Unanswered);
        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// В архивном проекте приём запрещён — как и остальные операции с проживанием. Проверки
    /// активности здесь раньше не было вовсе.
    /// </summary>
    [Fact]
    public async Task AcceptInvite_InArchivedProject_Throws()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий");
        var receiver = CreateClaim("Приглашаемый");
        var invite = mock.CreateAccommodationInvite(sender, receiver);
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().AcceptAccommodationInvite(InviteId(invite)));

        invite.IsAccepted.ShouldBe(InviteState.Unanswered);
        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    #region DeclineAccommodationInvite / CancelAccommodationInvite

    /// <summary>Приглашённый отказывается от приглашения.</summary>
    [Fact]
    public async Task DeclineInvite_MarksInviteDeclined()
    {
        var invite = CreateInviteBetweenNewClaims();

        await CreateService().DeclineAccommodationInvite(InviteId(invite));

        invite.IsAccepted.ShouldBe(InviteState.Declined);
        invite.ResolveDescription.ShouldBe(ResolveDescription.Declined);
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>Приглашающий отзывает отправленное приглашение.</summary>
    [Fact]
    public async Task CancelInvite_MarksInviteCanceled()
    {
        var invite = CreateInviteBetweenNewClaims();

        await CreateService().CancelAccommodationInvite(InviteId(invite));

        invite.IsAccepted.ShouldBe(InviteState.Canceled);
        invite.ResolveDescription.ShouldBe(ResolveDescription.Canceled);
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// У отказа и отзыва разные корни, и это видно по правам: <c>AccommodationChange</c> пускает
    /// игрока к <b>утверждённой</b> заявке, но не к неутверждённой. Игрок, которому принадлежат
    /// обе заявки, отзывает своё приглашение (его заявка утверждена) и не может отказаться за
    /// приглашённого, чья заявка ещё не утверждена.
    /// </summary>
    /// <remarks>
    /// До миграции отказ и отзыв были одним методом с параметром <c>InviteState</c>, то есть у
    /// обеих операций был один корень — хотя делают их разные люди.
    /// </remarks>
    [Fact]
    public async Task DeclineAndCancel_UseDifferentRoots()
    {
        var sender = CreateClaimWithAccommodation("Приглашающий, заявка утверждена");
        var receiver = mock.CreateClaim(mock.CreateCharacter("Приглашаемый"), mock.Player);
        var invite = mock.CreateAccommodationInvite(sender, receiver);

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(mock.Player.UserId).DeclineAccommodationInvite(InviteId(invite)));

        invite.IsAccepted.ShouldBe(InviteState.Unanswered);

        await CreateService(mock.Player.UserId).CancelAccommodationInvite(InviteId(invite));

        invite.IsAccepted.ShouldBe(InviteState.Canceled);
    }

    /// <summary>
    /// Отказаться или отозвать может только сторона приглашения или мастер с правом расселять.
    /// </summary>
    /// <remarks>
    /// До миграции проверки прав здесь не было вовсе: зная идентификатор приглашения, его мог
    /// отклонить любой аутентифицированный пользователь.
    /// </remarks>
    [Fact]
    public async Task DeclineAndCancelInvite_ByUnrelatedUser_Throws()
    {
        var invite = CreateInviteBetweenNewClaims();

        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(StrangerUserId).DeclineAccommodationInvite(InviteId(invite)));
        _ = await Should.ThrowAsync<NoAccessToProjectException>(
            () => CreateService(StrangerUserId).CancelAccommodationInvite(InviteId(invite)));

        invite.IsAccepted.ShouldBe(InviteState.Unanswered);
        SaveChangesCallCount.ShouldBe(0);
        notificationService.Invites.ShouldBeEmpty();
    }

    /// <summary>
    /// На приглашение уже ответили — второй ответ отклоняется. Раньше состояние не проверялось, и
    /// повторный отказ переписывал уже принятое приглашение.
    /// </summary>
    [Fact]
    public async Task DeclineInvite_AlreadyAnswered_Throws()
    {
        var invite = CreateInviteBetweenNewClaims();
        await CreateService().DeclineAccommodationInvite(InviteId(invite));

        var exception = await Should.ThrowAsync<AccommodationInviteNotAllowedException>(
            () => CreateService().CancelAccommodationInvite(InviteId(invite)));

        exception.Message.ShouldContain("уже ответили");
        invite.IsAccepted.ShouldBe(InviteState.Declined);
        SaveChangesCallCount.ShouldBe(1);
    }

    /// <summary>
    /// Приглашения с таким идентификатором нет. До миграции летело нетипизированное
    /// <see cref="Exception"/> с английским текстом «Invite request not found.».
    /// </summary>
    [Fact]
    public async Task DeclineInvite_UnknownInvite_Throws()
    {
        var unknownInviteId = new AccommodationInviteIdentification(mock.ProjectInfo.ProjectId, 100500);

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().DeclineAccommodationInvite(unknownInviteId));

        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>
    /// Приглашения с таким идентификатором нет. До миграции промах по идентификатору в приёме не
    /// проверялся вовсе и давал <see cref="NullReferenceException"/> — игрок видел «что-то пошло
    /// не так».
    /// </summary>
    [Fact]
    public async Task AcceptInvite_UnknownInvite_Throws()
    {
        var unknownInviteId = new AccommodationInviteIdentification(mock.ProjectInfo.ProjectId, 100500);

        _ = await Should.ThrowAsync<JoinRpgEntityNotFoundException>(
            () => CreateService().AcceptAccommodationInvite(unknownInviteId));

        SaveChangesCallCount.ShouldBe(0);
    }

    /// <summary>В архивном проекте отказ и отзыв запрещены, как и остальное проживание.</summary>
    [Fact]
    public async Task DeclineAndCancelInvite_InArchivedProject_Throw()
    {
        var invite = CreateInviteBetweenNewClaims();
        ArchiveProject();

        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().DeclineAccommodationInvite(InviteId(invite)));
        _ = await Should.ThrowAsync<ProjectDeactivatedException>(
            () => CreateService().CancelAccommodationInvite(InviteId(invite)));

        invite.IsAccepted.ShouldBe(InviteState.Unanswered);
        SaveChangesCallCount.ShouldBe(0);
    }

    #endregion

    private AccommodationInvite CreateInviteBetweenNewClaims()
        => mock.CreateAccommodationInvite(
            CreateClaimWithAccommodation("Приглашающий"),
            CreateClaim("Приглашаемый"));
}
