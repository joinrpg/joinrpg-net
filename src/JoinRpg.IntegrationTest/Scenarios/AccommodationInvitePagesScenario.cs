using System.Data.Entity;
using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims.Accommodation;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.ProjectMetadata;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Accommodation;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Контур приглашений к совместному проживанию целиком: кого можно пригласить, создание, приём,
/// отказ, отзыв — через ajax-ручки <c>/webapi/AccommodationInvite/</c>, которыми ходит остров
/// <c>AccommodationInviteControl</c>.
/// </summary>
/// <remarks>
/// Приглашениями распоряжается сам игрок (#5656): приглашает игрок заявки-отправителя, отвечает
/// игрок заявки-получателя, и каждый ходит своим клиентом. Мастер с правом расселять вместо
/// приглашения сразу поселяет вместе (<c>MergeGroups</c>) — это тут проверено ровно настолько,
/// чтобы было видно, что ручка доходит до сервиса и что мастеру приглашать не дают.
/// <para>
/// Поведение контура подробно покрыто юнит-тестами поверх фейков
/// (<c>src/JoinRpg.Services.Impl.Test/Accommodation/</c>), и дублировать их тут нечего. Смысл этого
/// сценария ровно один: прогнать на настоящем SQL Server те EF6-запросы, которых фейки не видят.
/// Переезд контура на <c>ICharacterPropsService</c> (ADR014) завёл для него новые загрузчики, и
/// ошибка в дереве выражений прошла бы все юнит-тесты, а упала бы в бою:
/// </para>
/// <list type="bullet">
/// <item><description>
/// <c>CharacterAggregateWriteRepository.ClaimQuery</c> в роли загрузчика стороны приглашения:
/// <c>LoadOtherClaim</c> отдаёт заявку вместе с трекаемой группой, по ним строится ссылка на группу
/// (<c>LoadOtherClaimAccommodation</c>), и в эту же группу при приёме переезжают внешним ключом.
/// Заявки, переезжающие вместе с принимающим, берутся из состава его группы, загруженного
/// <c>Include(c =&gt; c.AccommodationRequest!.Subjects)</c>. И <c>LoadInvite</c>;
/// </description></item>
/// <item><description>
/// решения по снимкам (ADR022 §4): план поселения внутри мутации
/// (<c>IAggregateMutationScope.LoadRoomCategoryPlan</c>);
/// </description></item>
/// <item><description>
/// <c>AccommodationInviteRepositoryImpl.GetInviteParticipants</c> — проекция в анонимный тип с
/// фильтром по проекту; через него проходит каждый ответ на приглашение;
/// </description></item>
/// <item><description>
/// список целей приглашения (ADR022): загрузчик плана поселения и запросы заголовков
/// <c>IClaimsRepository.GetApprovedClaimHeaders</c> и <c>GetApprovedClaimHeadersWithoutAccommodation</c>;
/// </description></item>
/// <item><description>
/// ветка расчёта свободного места <b>по комнате</b> (<c>RoomCategoryPlan.GetFreeSpaceForGroup</c>
/// у расселённой группы) — при приёме, когда приглашающий уже заселён; жильцов комнаты план читает
/// своей проекцией, без <c>Include</c>.
/// </description></item>
/// </list>
/// Сид тут свой, не смоучный: смоук один на все свои сценарии и только читает, а приглашения
/// переселяют заявки между группами — это ломало бы его проверки и зависело бы от порядка тестов.
/// </remarks>
public class AccommodationInvitePagesScenario(JoinApplicationFactory factory)
    : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Вместимость типа проживания. Трёх мест хватает, чтобы собрать группу из двоих и пригласить
    /// её целиком третьей заявкой — иначе путь «приглашение группы» не выражается.
    /// </summary>
    private const int RoomCapacity = 3;

    /// <summary>
    /// Страница, с которой берётся antiforgery-токен: токен привязан к cookie клиента, а не к
    /// странице, поэтому подходит любая форма. Форма создания проекта доступна любому вошедшему
    /// пользователю и не тащит за собой чужих ленивых загрузок.
    /// </summary>
    private const string AntiforgeryTokenPage = "game/create";

    /// <summary>
    /// Без antiforgery-токена /webapi отвечает 400 с причиной, а не редиректом на страницу ошибки:
    /// остров ходит через fetch, и редирект превратился бы в 200 — отказ выглядел бы успехом.
    /// </summary>
    [Fact]
    public async Task CreateInvite_WithoutAntiforgeryToken_IsRejectedWithReason()
    {
        var seed = await SeedAsync();
        var sessions = new Sessions(factory, seed);
        var sender = seed.Claims[0];
        var senderPlayer = await sessions.PlayerOf(sender);
        var receiverTarget = FindTarget(await GetTargetsAsync(senderPlayer.Client, sender), seed.GroupTarget(1));

        var response = await senderPlayer.Client.PostAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", receiverTarget.TargetId.ToString())),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await DescribeAsync(response, "Ожидался отказ antiforgery"));
        (await response.Content.ReadAsStringAsync()).ShouldContain("Сессия устарела");
        var receiverPlayer = await sessions.PlayerOf(seed.Claims[1]);
        (await GetInvitesAsync(receiverPlayer.Client, seed.Claims[1], InviteDirection.Incoming)).ShouldBeEmpty(
            "Приглашение без токена не должно было создаться");
    }

    /// <summary>
    /// Полный круг «создали — увидели — приняли», оба вида цели приглашения.
    /// </summary>
    /// <remarks>
    /// Сначала приглашается заявка, у которой тип проживания уже выбран: в списке целей такая
    /// приходит заявкой на проживание (группой из одного человека), и создание находит её в плане
    /// поселения приглашающего. Потом — заявка вообще без типа проживания: это цель-заявка, и у неё
    /// работает ссылка чужой заявки на группу (<c>LoadOtherClaimAccommodation</c>). Результат наблюдается там же, где его
    /// видит игрок: у приглашающего уменьшается свободное место, а переехавший перестаёт быть
    /// доступной целью.
    /// </remarks>
    [Fact]
    public async Task CreateAndAcceptInvite_RoundTrip()
    {
        var seed = await SeedAsync();
        var sessions = new Sessions(factory, seed);

        var sender = seed.Claims[0];
        var receiver = seed.Claims[1];
        var senderPlayer = await sessions.PlayerOf(sender);
        var receiverPlayer = await sessions.PlayerOf(receiver);

        var targetsBefore = await GetTargetsAsync(senderPlayer.Client, sender);
        targetsBefore.Mode.ShouldBe(
            AccommodationInviteMode.Invite,
            "Игрок своей заявки приглашает, а не поселяет вместе");
        targetsBefore.SenderRequestId.ShouldBe(
            seed.RequestId(0),
            "Заявка на проживание приглашающего должна быть его собственной");
        targetsBefore.RoomFreeSpace.ShouldBe(
            RoomCapacity - 1,
            "В группе приглашающего он один, значит свободны все места кроме его собственного");

        var receiverTarget = FindTarget(targetsBefore, seed.GroupTarget(1));

        var create = await senderPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", receiverTarget.TargetId.ToString())),
            senderPlayer.Token);
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(create, "Приглашение не создалось"));

        var incoming = await GetInvitesAsync(receiverPlayer.Client, receiver, InviteDirection.Incoming);
        var invite = incoming.ShouldHaveSingleItem();
        invite.State.ShouldBe(InviteState.Unanswered, "Только что созданное приглашение ещё без ответа");
        invite.CanRespond.ShouldBeTrue("Получатель — сам игрок, отвечать на приглашение ему");

        var accept = await receiverPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", invite.InviteId.ToString())),
            receiverPlayer.Token);
        accept.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(accept, "Приглашение не принялось"));

        var targetsAfterAccept = await GetTargetsAsync(senderPlayer.Client, sender);
        targetsAfterAccept.SenderRequestId.ShouldBe(
            seed.RequestId(0),
            "Переезжает приглашённый, группа приглашающего остаётся той же");
        targetsAfterAccept.RoomFreeSpace.ShouldBe(
            RoomCapacity - 2,
            "В группе приглашающего стало двое — свободное место должно уменьшиться");
        targetsAfterAccept.Targets.Select(target => target.TargetId).ShouldNotContain(
            receiverTarget.TargetId,
            customMessage: "Приглашать того, кто уже живёт вместе с нами, больше нельзя");

        // Вторая цель — заявка, у которой тип проживания не выбран вовсе. Это ровно тот случай,
        // когда группы у приглашаемого нет и загрузчик обязан вернуть null, а не упасть.
        var noRequestTarget = FindTarget(
            targetsAfterAccept,
            AccommodationGroupIdentification.From(seed.ClaimWithoutRequest));

        var createSecond = await senderPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", noRequestTarget.TargetId.ToString())),
            senderPlayer.Token);
        createSecond.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(createSecond, "Приглашение заявки без типа проживания не создалось"));

        var noRequestPlayer = await sessions.PlayerOf(seed.ClaimWithoutRequest);
        var incomingSecond = await GetInvitesAsync(
            noRequestPlayer.Client, seed.ClaimWithoutRequest, InviteDirection.Incoming);
        var acceptSecond = await noRequestPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", incomingSecond.ShouldHaveSingleItem().InviteId.ToString())),
            noRequestPlayer.Token);
        acceptSecond.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(acceptSecond, "Приглашение заявки без типа проживания не принялось"));

        var targetsFull = await GetTargetsAsync(senderPlayer.Client, sender);
        targetsFull.RoomFreeSpace.ShouldBe(0, "Номер заполнен: в группе все три места заняты");
        targetsFull.Targets.ShouldBeEmpty("В полный номер приглашать некого");
    }

    /// <summary>
    /// Приглашается не заявка, а сложившаяся группа соседей целиком.
    /// </summary>
    /// <remarks>
    /// Цель приглашения — заявка на проживание, в которой
    /// уже двое, и приглашение уходит каждому её участнику. Проверяется и то, чего нет у
    /// приглашения одной заявки: после приёма одним участником второе приглашение не остаётся
    /// висеть неотвеченным — его закрывает разбор приглашений переехавших.
    /// </remarks>
    [Fact]
    public async Task CreateInvite_ToWholeGroup()
    {
        var seed = await SeedAsync();
        var sessions = new Sessions(factory, seed);

        // Готовим группу из двоих: первая заявка приглашает вторую, вторая соглашается.
        await InviteAndAcceptAsync(sessions, seed, senderIndex: 0, receiverTarget: seed.GroupTarget(1));

        var outsider = seed.Claims[2];
        var outsiderPlayer = await sessions.PlayerOf(outsider);
        var targetsOfOutsider = await GetTargetsAsync(outsiderPlayer.Client, outsider);
        var groupTarget = FindTarget(targetsOfOutsider, seed.GroupTarget(0));

        groupTarget.Subtext.ShouldNotBeNullOrWhiteSpace(
            "У группы из двоих в списке должна быть поясняющая подпись — иначе её не отличить от одиночки");
        groupTarget.Text.ShouldContain(
            ",",
            customMessage: "В строке группы перечисляются имена всех её участников");
        groupTarget.TargetId.AsAccommodationRequestId().ShouldBe(
            seed.RequestId(0),
            "Цель-группа — это заявка на проживание, а не заявка игрока");

        var create = await outsiderPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", outsider.ToString()), ("target", groupTarget.TargetId.ToString())),
            outsiderPlayer.Token);
        create.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(create, "Приглашение группы не создалось"));

        var outgoing = await GetInvitesAsync(outsiderPlayer.Client, outsider, InviteDirection.Outgoing);
        outgoing.Count.ShouldBe(2, "Приглашение группы уходит каждому её участнику");

        // Принимает один участник группы — переезжает вся группа.
        var firstMember = await sessions.PlayerOf(seed.Claims[0]);
        var incoming = await GetInvitesAsync(firstMember.Client, seed.Claims[0], InviteDirection.Incoming);
        var accept = await firstMember.Client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", incoming.ShouldHaveSingleItem().InviteId.ToString())),
            firstMember.Token);
        accept.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(accept, "Приглашение группы не принялось"));

        var targetsAfter = await GetTargetsAsync(outsiderPlayer.Client, outsider);
        targetsAfter.SenderRequestId.ShouldBe(
            seed.RequestId(2),
            "Группа приглашающего осталась его собственной, в неё переехали остальные");
        targetsAfter.RoomFreeSpace.ShouldBe(0, "Переехала вся группа — номер заполнен");

        var secondMember = await sessions.PlayerOf(seed.Claims[1]);
        var secondMemberInvites = await GetInvitesAsync(secondMember.Client, seed.Claims[1], InviteDirection.Incoming);
        secondMemberInvites.ShouldBeEmpty(
            "Приглашение второму участнику сбылось вместе с переездом и висеть неотвеченным не должно");
    }

    /// <summary>
    /// Свободное место считается по комнате, когда группа приглашающего уже расселена.
    /// </summary>
    /// <remarks>
    /// Порядок шагов вынужденный: приглашать, когда кто-то расселён, запрещено
    /// (<c>EnsureCanInvite</c>), поэтому приглашение создаётся до заселения, а принимается уже
    /// после. Только в этот момент <c>RoomCategoryPlan.GetFreeSpaceForGroup</c> уходит в ветку
    /// комнаты и считает место по её жильцам из плана поселения. Результат виден на
    /// странице комнат: приглашённый въехал к приглашающему. Заселяет и смотрит комнаты владелец
    /// проекта — это мастерская страница.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_WhenSenderIsOccupied_CountsFreeSpaceByRoom()
    {
        var seed = await SeedAsync();
        var sessions = new Sessions(factory, seed);

        var sender = seed.Claims[0];
        var receiver = seed.Claims[1];
        var senderPlayer = await sessions.PlayerOf(sender);
        var receiverPlayer = await sessions.PlayerOf(receiver);
        var owner = await sessions.Owner();

        var targets = await GetTargetsAsync(senderPlayer.Client, sender);
        var receiverTarget = FindTarget(targets, seed.GroupTarget(1));

        var create = await senderPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", receiverTarget.TargetId.ToString())),
            senderPlayer.Token);
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(create, "Приглашение не создалось"));

        // Заселяем группу приглашающего в комнату — той же ручкой, которой это делает страница комнат.
        var occupy = await owner.Client.OccupyAsync(
            owner.Token, seed.ProjectId, new AccommodationRoomIdentification(seed.ProjectId, seed.RoomIds[0]), seed.RequestId(0));
        occupy.StatusCode.ShouldBe(HttpStatusCode.OK, "Заселение приглашающего не прошло");
        (await seed.GetOccupancyAsync(owner.Client, seed.RoomIds[0])).ShouldBe(
            1,
            "В комнате пока только приглашающий");

        var incoming = await GetInvitesAsync(receiverPlayer.Client, receiver, InviteDirection.Incoming);
        var accept = await receiverPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", incoming.ShouldHaveSingleItem().InviteId.ToString())),
            receiverPlayer.Token);
        accept.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(accept, "Приём приглашения в расселённую группу не прошёл"));

        (await seed.GetOccupancyAsync(owner.Client, seed.RoomIds[0])).ShouldBe(
            2,
            "Приглашённый должен переехать в комнату приглашающего");
    }

    /// <summary>
    /// Отказ и отзыв: приглашение не исчезает, а остаётся в списке со своим состоянием.
    /// </summary>
    /// <remarks>
    /// Отказ и отзыв делают разные стороны, и после переезда на <c>ICharacterPropsService</c> у них
    /// разные корни агрегата — а значит и разные обращения к
    /// <c>GetInviteParticipants</c>/<c>LoadInvite</c>. Поэтому обе операции проверяются рядом:
    /// отказывается игрок получателя, отзывает игрок отправителя.
    /// </remarks>
    [Fact]
    public async Task DeclineAndCancelInvite_KeepInviteVisibleWithState()
    {
        var seed = await SeedAsync();
        var sessions = new Sessions(factory, seed);

        var sender = seed.Claims[0];
        var senderPlayer = await sessions.PlayerOf(sender);
        var receiverPlayer = await sessions.PlayerOf(seed.Claims[1]);

        var declined = await CreateInviteAsync(sessions, seed, senderIndex: 0, target: seed.GroupTarget(1));
        var decline = await receiverPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("DeclineInvite", ("inviteId", declined.ToString())),
            receiverPlayer.Token);
        decline.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(decline, "Отказ не прошёл"));

        var receiverInvites = await GetInvitesAsync(receiverPlayer.Client, seed.Claims[1], InviteDirection.Incoming);
        receiverInvites.ShouldHaveSingleItem().State.ShouldBe(
            InviteState.Declined,
            "Отклонённое приглашение остаётся видимым, но со своим состоянием");

        var canceled = await CreateInviteAsync(sessions, seed, senderIndex: 0, target: seed.GroupTarget(2));
        var cancel = await senderPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("CancelInvite", ("inviteId", canceled.ToString())),
            senderPlayer.Token);
        cancel.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(cancel, "Отзыв не прошёл"));

        var outgoing = await GetInvitesAsync(senderPlayer.Client, sender, InviteDirection.Outgoing);
        outgoing.Select(invite => invite.State).Order().ShouldBe(
            new[] { InviteState.Declined, InviteState.Canceled }.Order(),
            "В отправленных должны остаться оба приглашения — отклонённое и отозванное");
    }

    /// <summary>
    /// Повторный ответ на то же приглашение — это 400 с внятным текстом, а не 500.
    /// </summary>
    /// <remarks>
    /// Так выглядит двойной клик по кнопке в острове. Проверка состояния живёт в сервисе, а
    /// раскладку в код ответа делает контроллер — это видно только сквозным запросом.
    /// </remarks>
    [Fact]
    public async Task AnswerInviteTwice_IsBadRequestWithMessage()
    {
        var seed = await SeedAsync();
        var sessions = new Sessions(factory, seed);
        var receiverPlayer = await sessions.PlayerOf(seed.Claims[1]);

        var inviteId = await CreateInviteAsync(sessions, seed, senderIndex: 0, target: seed.GroupTarget(1));

        var decline = await receiverPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("DeclineInvite", ("inviteId", inviteId.ToString())),
            receiverPlayer.Token);
        decline.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(decline, "Первый отказ не прошёл"));

        foreach (var action in new[] { "DeclineInvite", "AcceptInvite" })
        {
            var again = await receiverPlayer.Client.PostWithTokenHeaderAsync(
                InviteUrl(action, ("inviteId", inviteId.ToString())),
                receiverPlayer.Token);

            again.StatusCode.ShouldBe(
                HttpStatusCode.BadRequest,
                $"Повторный ответ ({action}) должен давать 400, а не падать 500-й");
            (await again.Content.ReadAsStringAsync()).ShouldNotBeNullOrWhiteSpace(
                $"Причина отказа ({action}) предназначена игроку, поэтому текст должен быть непустым");
        }
    }

    /// <summary>
    /// Мастер с правом расселять (владелец проекта) поселяет игроков вместе одной ручкой, без
    /// приглашения: цель сразу переезжает к заявке, и приглашений не появляется.
    /// </summary>
    [Fact]
    public async Task MergeGroups_ByMaster_MovesTargetWithoutInvite()
    {
        var seed = await SeedAsync();
        var sessions = new Sessions(factory, seed);
        var owner = await sessions.Owner();

        var host = seed.Claims[0];
        var targetsBefore = await GetTargetsAsync(owner.Client, host);
        targetsBefore.Mode.ShouldBe(
            AccommodationInviteMode.Merge,
            "Владелец проекта вправе расселять — ему предлагается поселить вместе, а не пригласить");

        var guestTarget = FindTarget(targetsBefore, seed.GroupTarget(1));

        var merge = await owner.Client.PostWithTokenHeaderAsync(
            InviteUrl("MergeGroups", ("claimId", host.ToString()), ("target", guestTarget.TargetId.ToString())),
            owner.Token);
        merge.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(merge, "Объединение не прошло"));

        var targetsAfter = await GetTargetsAsync(owner.Client, host);
        targetsAfter.SenderRequestId.ShouldBe(
            seed.RequestId(0),
            "Переезжает цель, группа заявки остаётся той же");
        targetsAfter.RoomFreeSpace.ShouldBe(
            RoomCapacity - 2,
            "В группе стало двое — свободное место должно уменьшиться");
        targetsAfter.Targets.Select(target => target.TargetId).ShouldNotContain(
            guestTarget.TargetId,
            customMessage: "Того, кто уже живёт вместе с заявкой, поселять к ней больше нельзя");

        // Прямо по базе: GetInvites прячет приглашения внутри своей группы и пропустил бы созданные.
        using var scope = factory.Services.CreateScope();
        (await scope.ServiceProvider.GetRequiredService<MyDbContext>().Set<AccommodationInvite>()
            .CountAsync(invite => invite.ProjectId == seed.ProjectId.Value))
            .ShouldBe(0, "Объединение мастером обходится без приглашений");
    }

    /// <summary>
    /// Мастер не приглашает от имени игрока: ручка создания приглашения отвечает 400 с причиной,
    /// и приглашение не создаётся (#5656).
    /// </summary>
    [Fact]
    public async Task CreateInvite_ByMaster_IsRejectedWithReason()
    {
        var seed = await SeedAsync();
        var sessions = new Sessions(factory, seed);
        var owner = await sessions.Owner();

        var sender = seed.Claims[0];
        var receiverTarget = FindTarget(await GetTargetsAsync(owner.Client, sender), seed.GroupTarget(1));

        var create = await owner.Client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", receiverTarget.TargetId.ToString())),
            owner.Token);

        create.StatusCode.ShouldBe(
            HttpStatusCode.BadRequest,
            await DescribeAsync(create, "Мастеру приглашать от имени игрока нельзя"));
        (await create.Content.ReadAsStringAsync()).ShouldContain("сам игрок");
        (await GetInvitesAsync(owner.Client, seed.Claims[1], InviteDirection.Incoming)).ShouldBeEmpty(
            "Отклонённое приглашение не должно было создаться");
    }

    /// <summary>
    /// Создаёт приглашение от игрока заявки-отправителя и возвращает его идентификатор, взятый из
    /// списка полученных у игрока заявки-получателя.
    /// </summary>
    private static async Task<AccommodationInviteIdentification> CreateInviteAsync(
        Sessions sessions,
        InviteSeed seed,
        int senderIndex,
        AccommodationGroupIdentification target)
    {
        var sender = seed.Claims[senderIndex];
        var senderPlayer = await sessions.PlayerOf(sender);
        var targets = await GetTargetsAsync(senderPlayer.Client, sender);
        var found = FindTarget(targets, target);

        var create = await senderPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", found.TargetId.ToString())),
            senderPlayer.Token);
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(create, "Приглашение не создалось"));

        // Идентификатор приглашения берётся из списка у получателя: наружу его не возвращает ни
        // ручка создания, ни сервис.
        var receiverClaimId = ReceiverOf(seed, target);
        var receiverPlayer = await sessions.PlayerOf(receiverClaimId);
        var incoming = await GetInvitesAsync(receiverPlayer.Client, receiverClaimId, InviteDirection.Incoming);

        return incoming
            .Where(invite => invite.State == InviteState.Unanswered)
            .ToList()
            .ShouldHaveSingleItem()
            .InviteId;
    }

    /// <summary>
    /// Создаёт приглашение и сразу его принимает игрок получателя — подготовка состояния для теста.
    /// </summary>
    private static async Task InviteAndAcceptAsync(
        Sessions sessions,
        InviteSeed seed,
        int senderIndex,
        AccommodationGroupIdentification receiverTarget)
    {
        var inviteId = await CreateInviteAsync(sessions, seed, senderIndex, receiverTarget);
        var receiverPlayer = await sessions.PlayerOf(ReceiverOf(seed, receiverTarget));

        var accept = await receiverPlayer.Client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", inviteId.ToString())),
            receiverPlayer.Token);
        accept.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(accept, "Приглашение не принялось"));
    }

    /// <summary>
    /// Заявка, которая получает приглашение: сама цель-заявка либо исходный хозяин цели-группы.
    /// </summary>
    private static ClaimIdentification ReceiverOf(InviteSeed seed, AccommodationGroupIdentification target)
        => target.AsAccommodationRequestId() is { } requestId
            ? seed.ClaimByRequest(requestId)
            : target.AsClaimId()!;

    private static async Task<AccommodationInviteTargetsViewModel> GetTargetsAsync(
        HttpClient client,
        ClaimIdentification claimId)
    {
        var url = InviteUrl("GetInviteTargets", ("claimId", claimId.ToString()));
        var response = await client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Список целей приглашения не открылся: {url}");

        return (await response.Content.ReadFromJsonAsync<AccommodationInviteTargetsViewModel>())
            .ShouldNotBeNull("Ручка целей приглашения вернула пустое тело");
    }

    private static async Task<IReadOnlyList<AccommodationInviteViewModel>> GetInvitesAsync(
        HttpClient client,
        ClaimIdentification claimId,
        InviteDirection direction)
    {
        var url = InviteUrl(
            "GetInvites",
            ("claimId", claimId.ToString()),
            ("direction", direction.ToString()));
        var response = await client.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Список приглашений не открылся: {url}");

        return (await response.Content.ReadFromJsonAsync<List<AccommodationInviteViewModel>>())
            .ShouldNotBeNull("Ручка списка приглашений вернула пустое тело");
    }

    /// <summary>
    /// Адрес ajax-ручки приглашений. Идентификаторы уезжают целиком, а не голым числом: в маршруте
    /// <c>/webapi/AccommodationInvite/[action]</c> проекта нет, и <c>ProjectEntityIdModelBinder</c>
    /// склеить число с проектом не сможет.
    /// </summary>
    private static string InviteUrl(string action, params (string Name, string Value)[] parameters)
        => $"webapi/AccommodationInvite/{action}?"
            + string.Join('&', parameters.Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value)}"));

    /// <summary>
    /// Цель приглашения из ответа ручки — именно из ответа, а не собранная руками: так тест заодно
    /// проверяет, что остров и сервер понимают идентификатор одинаково.
    /// </summary>
    private static AccommodationInviteTargetViewModel FindTarget(
        AccommodationInviteTargetsViewModel model,
        AccommodationGroupIdentification expected)
        => model.Targets.SingleOrDefault(target => target.TargetId == expected)
            ?? throw new InvalidOperationException(
                $"Среди целей приглашения нет {expected}. Есть: "
                + (model.Targets.Count == 0
                    ? "ничего"
                    : string.Join(", ", model.Targets.Select(target => target.TargetId))));

    /// <summary>
    /// Описание неудачного ответа: у ручек приглашений причина отказа лежит в теле, и без неё
    /// падение теста выглядит как «ожидался 200, получен 400» без всякого объяснения.
    /// </summary>
    private static async Task<string> DescribeAsync(HttpResponseMessage response, string what)
        => response.IsSuccessStatusCode
            ? what
            : $"{what}: ответ {(int)response.StatusCode}, тело «{await response.Content.ReadAsStringAsync()}»";

    /// <summary>
    /// Поднимает отдельный проект с поселением: тип проживания на <see cref="RoomCapacity"/> мест,
    /// две комнаты, три утверждённые заявки с выбранным типом проживания и одна — без него.
    /// </summary>
    /// <remarks>
    /// Заявки именно утверждённые: список потенциальных соседей собирается только из утверждённых
    /// (<c>IClaimsRepository.GetApprovedClaimHeaders</c>, <c>GetApprovedClaimHeadersWithoutAccommodation</c>),
    /// и на неутверждённых ручка целей вернула бы пустой список — приглашать было бы некого. К тому
    /// же игрок меняет проживание только у утверждённой заявки.
    /// Четвёртая заявка без типа проживания нужна для второго вида цели: приглашение заявки, у
    /// которой группы ещё нет.
    /// </remarks>
    private async Task<InviteSeed> SeedAsync()
    {
        var (ownerId, ownerEmail) = await CreateUserAsync();

        ProjectIdentification projectId;
        using (var scope = factory.Services.CreateScope())
        {
            projectId = await TestUserProjectHelpers.CreateProjectAsync(
                scope.ServiceProvider, ownerId, "Проект для приглашений к проживанию");
        }

        await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);

            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });
            await projectService.SetAccommodationSettings(projectId, enableAccommodation: true);
        });

        var roomTypeId = await factory.Services.RunAsAsync(ownerId, sp =>
            sp.GetRequiredService<IAccommodationTypeService>().CreateAccommodationType(
                projectId,
                new AccommodationTypeRequest(
                    "Трёшка",
                    new MarkdownString("Комната на троих"),
                    Cost: 100,
                    Capacity: RoomCapacity,
                    IsPlayerSelectable: true)));

        // Комнаты создаются в категории, а не в типе проживания: категорию по типу знают только
        // метаданные (ADR018, §2).
        var roomIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IAccommodationService>().AddRooms(
                projectInfo.AccommodationSettings.GetTypeById(roomTypeId).RoomCategoryId,
                ["1", "2"]);
        });

        const int ClaimCount = 4;

        var characters = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                .GetProjectMetadata(projectId);
            var characterService = sp.GetRequiredService<ICharacterService>();
            var result = new List<CharacterIdentification>(ClaimCount);
            for (var i = 0; i < ClaimCount; i++)
            {
                result.Add(await characterService.AddCharacter(new AddCharacterRequest(
                    projectId,
                    ParentCharacterGroupIds: [],
                    new CharacterTypeInfo(
                        CharacterType.Player,
                        IsHot: false,
                        SlotLimit: null,
                        SlotName: null,
                        CharacterVisibility.Public),
                    FieldValues: FieldLayerContainer.Empty(projectInfo))));
            }

            return result;
        });

        var claims = new List<ClaimIdentification>(characters.Count);
        var playerEmails = new Dictionary<ClaimIdentification, string>(characters.Count);
        foreach (var characterId in characters)
        {
            var (playerId, playerEmail) = await CreateUserAsync();
            var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                    characterId,
                    "Хочу играть эту роль",
                    FieldLayerContainer.Empty(projectInfo),
                    sensitiveDataAllowed: true);
            });
            claims.Add(claimId);
            playerEmails.Add(claimId, playerEmail);
        }

        var requestIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            foreach (var claimId in claims)
            {
                await claimService.ApproveByMaster(claimId, "Принято");
            }

            // Тип проживания выбран у всех, кроме последней заявки: она остаётся целью-заявкой.
            var withType = claims.SkipLast(1).ToList();
            foreach (var claimId in withType)
            {
                await claimService.SetAccommodationType(
                    projectId.Value, claimId.ClaimId, roomTypeId.AccommodationTypeId);
            }

            var groupIds = await AccommodationTestHelpers.GetAccommodationGroupIdsAsync(sp, withType);
            return groupIds.Select(groupId => groupId.AccommodationRequestId).ToList();
        });

        return new InviteSeed(
            ownerId,
            ownerEmail,
            projectId,
            roomTypeId.AccommodationTypeId,
            [.. roomIds.Select(room => room.RoomId)],
            [.. claims.SkipLast(1)],
            requestIds,
            claims[^1],
            playerEmails);
    }

    private async Task<(UserIdentification UserId, string Email)> CreateUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        return await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
    }

    /// <summary>Проект с поселением и заявками, поднятый под один тест.</summary>
    /// <param name="Claims">Утверждённые заявки с выбранным типом проживания.</param>
    /// <param name="RequestIds">Их заявки на проживание, по индексам <paramref name="Claims"/>.</param>
    /// <param name="ClaimWithoutRequest">Утверждённая заявка, у которой тип проживания не выбран.</param>
    /// <param name="PlayerEmails">Email игрока каждой заявки — чтобы войти от его имени.</param>
    private sealed record InviteSeed(
        UserIdentification OwnerId,
        string OwnerEmail,
        ProjectIdentification ProjectId,
        int RoomTypeId,
        IReadOnlyList<int> RoomIds,
        IReadOnlyList<ClaimIdentification> Claims,
        IReadOnlyList<int> RequestIds,
        ClaimIdentification ClaimWithoutRequest,
        IReadOnlyDictionary<ClaimIdentification, string> PlayerEmails)
    {
        /// <summary>Заявка на проживание указанной заявки.</summary>
        public AccommodationRequestIdentification RequestId(int claimIndex)
            => new(ProjectId, RequestIds[claimIndex]);

        /// <summary>Цель приглашения «вся группа указанной заявки».</summary>
        public AccommodationGroupIdentification GroupTarget(int claimIndex)
            => AccommodationGroupIdentification.From(RequestId(claimIndex));

        /// <summary>Заявка, которой изначально принадлежит эта заявка на проживание.</summary>
        public ClaimIdentification ClaimByRequest(AccommodationRequestIdentification requestId)
            => Claims[RequestIds.ToList().IndexOf(requestId.AccommodationRequestId)];

        /// <summary>Страница комнат типа проживания.</summary>
        public string RoomTypeDetailsUrl => $"{ProjectId.Value}/rooms/{RoomTypeId}/details";

        /// <summary>Сколько человек живёт в комнате по данным страницы комнат</summary>
        public Task<int> GetOccupancyAsync(HttpClient client, int roomId)
            => client.GetOccupancyAsync(RoomTypeDetailsUrl, roomId);
    }

    /// <summary>Вошедший пользователь: клиент со своими cookie и antiforgery-токен к ним.</summary>
    private sealed record Session(HttpClient Client, string Token);

    /// <summary>
    /// Вошедшие участники одного теста: владелец проекта и игроки заявок. Каждый входит один раз,
    /// при первом обращении. Antiforgery-токен у каждого свой — он привязан к cookie клиента.
    /// </summary>
    private sealed class Sessions(JoinApplicationFactory factory, InviteSeed seed)
    {
        private readonly Dictionary<string, Session> sessions = [];

        /// <summary>Владелец проекта — мастер с правом расселять.</summary>
        public Task<Session> Owner() => LoginAsync(seed.OwnerEmail);

        /// <summary>Игрок указанной заявки.</summary>
        public Task<Session> PlayerOf(ClaimIdentification claimId) => LoginAsync(seed.PlayerEmails[claimId]);

        private async Task<Session> LoginAsync(string email)
        {
            if (!sessions.TryGetValue(email, out var session))
            {
                var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
                    factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
                    email,
                    followsRedirects: false);
                session = new Session(client, await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage));
                sessions.Add(email, session);
            }

            return session;
        }
    }
}
