using System.Net;
using JoinRpg.Common.PrimitiveTypes;
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
/// Поведение контура подробно покрыто юнит-тестами поверх фейков
/// (<c>src/JoinRpg.Services.Impl.Test/Accommodation/</c>), и дублировать их тут нечего. Смысл этого
/// сценария ровно один: прогнать на настоящем SQL Server те EF6-запросы, которых фейки не видят.
/// Переезд контура на <c>ICharacterPropsService</c> (ADR014) завёл для него новые загрузчики, и
/// ошибка в дереве выражений прошла бы все юнит-тесты, а упала бы в бою:
/// <list type="bullet">
/// <item><description>
/// <c>CharacterAggregateWriteRepository.AccommodationGroupQuery</c> — в нём
/// <c>Include(request =&gt; request.Accommodation!.Inhabitants.Select(group =&gt; group.Subjects))</c>,
/// то есть ссылочная навигация → коллекция → коллекция. Такой <c>Include</c> EF6 проверяет только
/// в рантайме;
/// </description></item>
/// <item><description>
/// три загрузчика поверх этого запроса: <c>LoadAccommodationGroupForClaim</c> (группа приглашающего
/// при приёме и группа приглашаемой заявки), <c>LoadAccommodationGroup</c> (приглашение целой
/// группы) и <c>LoadInvite</c>;
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
/// ветка расчёта свободного места <b>по комнате</b>
/// (<c>AccommodationExtensions.GetRoomFreeSpace(request, ProjectInfo)</c> при заполненном
/// <c>Accommodation</c>) — единственный путь, который читает жильцов комнаты, то есть
/// единственный, где вложенный <c>Include</c> выше вообще обязан был сработать.
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
    /// Полный круг «создали — увидели — приняли», оба вида цели приглашения.
    /// </summary>
    /// <remarks>
    /// Сначала приглашается заявка, у которой тип проживания уже выбран: в списке целей такая
    /// приходит заявкой на проживание (группой из одного человека), и создание идёт через
    /// <c>LoadAccommodationGroup</c>. Потом — заявка вообще без типа проживания: это цель-заявка, и
    /// у неё работает <c>LoadAccommodationGroupForClaim</c>. Результат наблюдается там же, где его
    /// видит игрок: у приглашающего уменьшается свободное место, а переехавший перестаёт быть
    /// доступной целью.
    /// </remarks>
    /// <summary>
    /// Без antiforgery-токена /webapi отвечает 400 с причиной, а не редиректом на страницу ошибки:
    /// остров ходит через fetch, и редирект превратился бы в 200 — отказ выглядел бы успехом.
    /// </summary>
    [Fact]
    public async Task CreateInvite_WithoutAntiforgeryToken_IsRejectedWithReason()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var sender = seed.Claims[0];
        var receiverTarget = FindTarget(await GetTargetsAsync(client, sender), seed.GroupTarget(1));

        var response = await client.PostAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", receiverTarget.TargetId.ToString())),
            content: null);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await DescribeAsync(response, "Ожидался отказ antiforgery"));
        (await response.Content.ReadAsStringAsync()).ShouldContain("Сессия устарела");
        (await GetInvitesAsync(client, seed.Claims[1], InviteDirection.Incoming)).ShouldBeEmpty(
            "Приглашение без токена не должно было создаться");
    }

    [Fact]
    public async Task CreateAndAcceptInvite_RoundTrip()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        var sender = seed.Claims[0];
        var receiver = seed.Claims[1];

        var targetsBefore = await GetTargetsAsync(client, sender);
        targetsBefore.SenderRequestId.ShouldBe(
            seed.RequestId(0),
            "Заявка на проживание приглашающего должна быть его собственной");
        targetsBefore.RoomFreeSpace.ShouldBe(
            RoomCapacity - 1,
            "В группе приглашающего он один, значит свободны все места кроме его собственного");

        var receiverTarget = FindTarget(targetsBefore, seed.GroupTarget(1));

        var create = await client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", receiverTarget.TargetId.ToString())),
            token);
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(create, "Приглашение не создалось"));

        var incoming = await GetInvitesAsync(client, receiver, InviteDirection.Incoming);
        var invite = incoming.ShouldHaveSingleItem();
        invite.State.ShouldBe(InviteState.Unanswered, "Только что созданное приглашение ещё без ответа");

        var accept = await client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", invite.InviteId.ToString())),
            token);
        accept.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(accept, "Приглашение не принялось"));

        var targetsAfterAccept = await GetTargetsAsync(client, sender);
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

        var createSecond = await client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", noRequestTarget.TargetId.ToString())),
            token);
        createSecond.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(createSecond, "Приглашение заявки без типа проживания не создалось"));

        var incomingSecond = await GetInvitesAsync(client, seed.ClaimWithoutRequest, InviteDirection.Incoming);
        var acceptSecond = await client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", incomingSecond.ShouldHaveSingleItem().InviteId.ToString())),
            token);
        acceptSecond.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(acceptSecond, "Приглашение заявки без типа проживания не принялось"));

        var targetsFull = await GetTargetsAsync(client, sender);
        targetsFull.RoomFreeSpace.ShouldBe(0, "Номер заполнен: в группе все три места заняты");
        targetsFull.Targets.ShouldBeEmpty("В полный номер приглашать некого");
    }

    /// <summary>
    /// Приглашается не заявка, а сложившаяся группа соседей целиком.
    /// </summary>
    /// <remarks>
    /// Это путь <c>LoadAccommodationGroup</c>: цель приглашения — заявка на проживание, в которой
    /// уже двое, и приглашение уходит каждому её участнику. Проверяется и то, чего нет у
    /// приглашения одной заявки: после приёма одним участником второе приглашение не остаётся
    /// висеть неотвеченным — его закрывает разбор приглашений переехавших.
    /// </remarks>
    [Fact]
    public async Task CreateInvite_ToWholeGroup()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        // Готовим группу из двоих: первая заявка приглашает вторую, вторая соглашается.
        await InviteAndAcceptAsync(client, token, seed, senderIndex: 0, receiverTarget: seed.GroupTarget(1));

        var outsider = seed.Claims[2];
        var targetsOfOutsider = await GetTargetsAsync(client, outsider);
        var groupTarget = FindTarget(targetsOfOutsider, seed.GroupTarget(0));

        groupTarget.Subtext.ShouldNotBeNullOrWhiteSpace(
            "У группы из двоих в списке должна быть поясняющая подпись — иначе её не отличить от одиночки");
        groupTarget.Text.ShouldContain(
            ",",
            customMessage: "В строке группы перечисляются имена всех её участников");
        groupTarget.TargetId.AsAccommodationRequestId().ShouldBe(
            seed.RequestId(0),
            "Цель-группа — это заявка на проживание, а не заявка игрока");

        var create = await client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", outsider.ToString()), ("target", groupTarget.TargetId.ToString())),
            token);
        create.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(create, "Приглашение группы не создалось"));

        var outgoing = await GetInvitesAsync(client, outsider, InviteDirection.Outgoing);
        outgoing.Count.ShouldBe(2, "Приглашение группы уходит каждому её участнику");

        // Принимает один участник группы — переезжает вся группа.
        var incoming = await GetInvitesAsync(client, seed.Claims[0], InviteDirection.Incoming);
        var accept = await client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", incoming.ShouldHaveSingleItem().InviteId.ToString())),
            token);
        accept.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(accept, "Приглашение группы не принялось"));

        var targetsAfter = await GetTargetsAsync(client, outsider);
        targetsAfter.SenderRequestId.ShouldBe(
            seed.RequestId(2),
            "Группа приглашающего осталась его собственной, в неё переехали остальные");
        targetsAfter.RoomFreeSpace.ShouldBe(0, "Переехала вся группа — номер заполнен");

        var secondMemberInvites = await GetInvitesAsync(client, seed.Claims[1], InviteDirection.Incoming);
        secondMemberInvites.ShouldBeEmpty(
            "Приглашение второму участнику сбылось вместе с переездом и висеть неотвеченным не должно");
    }

    /// <summary>
    /// Свободное место считается по комнате, когда группа приглашающего уже расселена.
    /// </summary>
    /// <remarks>
    /// Порядок шагов вынужденный: приглашать, когда кто-то расселён, запрещено
    /// (<c>EnsureCanInvite</c>), поэтому приглашение создаётся до заселения, а принимается уже
    /// после. Только в этот момент <c>GetRoomFreeSpace(request, ProjectInfo)</c> уходит в ветку
    /// комнаты и читает её жильцов — то есть только здесь работает вложенный
    /// <c>Include(... Inhabitants.Select(group =&gt; group.Subjects))</c>. Результат виден на
    /// странице комнат: приглашённый въехал к приглашающему.
    /// </remarks>
    [Fact]
    public async Task AcceptInvite_WhenSenderIsOccupied_CountsFreeSpaceByRoom()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        var sender = seed.Claims[0];
        var receiver = seed.Claims[1];

        var targets = await GetTargetsAsync(client, sender);
        var receiverTarget = FindTarget(targets, seed.GroupTarget(1));

        var create = await client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", receiverTarget.TargetId.ToString())),
            token);
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(create, "Приглашение не создалось"));

        // Заселяем группу приглашающего в комнату — той же ручкой, которой это делает rooms.js.
        var occupy = await client.PostWithTokenHeaderAsync(
            seed.OccupyUrl(seed.RoomIds[0], seed.RequestIds[0]),
            token);
        occupy.StatusCode.ShouldBe(HttpStatusCode.OK, "Заселение приглашающего не прошло");
        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(
            1,
            "В комнате пока только приглашающий");

        var incoming = await GetInvitesAsync(client, receiver, InviteDirection.Incoming);
        var accept = await client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", incoming.ShouldHaveSingleItem().InviteId.ToString())),
            token);
        accept.StatusCode.ShouldBe(
            HttpStatusCode.OK,
            await DescribeAsync(accept, "Приём приглашения в расселённую группу не прошёл"));

        (await seed.GetOccupancyAsync(client, seed.RoomIds[0])).ShouldBe(
            2,
            "Приглашённый должен переехать в комнату приглашающего");
    }

    /// <summary>
    /// Отказ и отзыв: приглашение не исчезает, а остаётся в списке со своим состоянием.
    /// </summary>
    /// <remarks>
    /// Отказ и отзыв делают разные стороны, и после переезда на <c>ICharacterPropsService</c> у них
    /// разные корни агрегата — а значит и разные обращения к
    /// <c>GetInviteParticipants</c>/<c>LoadInvite</c>. Поэтому обе операции проверяются рядом.
    /// </remarks>
    [Fact]
    public async Task DeclineAndCancelInvite_KeepInviteVisibleWithState()
    {
        var seed = await SeedAsync();
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        var sender = seed.Claims[0];

        var declined = await CreateInviteAsync(client, token, seed, senderIndex: 0, target: seed.GroupTarget(1));
        var decline = await client.PostWithTokenHeaderAsync(
            InviteUrl("DeclineInvite", ("inviteId", declined.ToString())),
            token);
        decline.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(decline, "Отказ не прошёл"));

        var receiverInvites = await GetInvitesAsync(client, seed.Claims[1], InviteDirection.Incoming);
        receiverInvites.ShouldHaveSingleItem().State.ShouldBe(
            InviteState.Declined,
            "Отклонённое приглашение остаётся видимым, но со своим состоянием");

        var canceled = await CreateInviteAsync(client, token, seed, senderIndex: 0, target: seed.GroupTarget(2));
        var cancel = await client.PostWithTokenHeaderAsync(
            InviteUrl("CancelInvite", ("inviteId", canceled.ToString())),
            token);
        cancel.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(cancel, "Отзыв не прошёл"));

        var outgoing = await GetInvitesAsync(client, sender, InviteDirection.Outgoing);
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
        var client = await seed.CreateClientAsync(factory);
        var token = await client.GetAntiforgeryTokenAsync(AntiforgeryTokenPage);

        var inviteId = await CreateInviteAsync(client, token, seed, senderIndex: 0, target: seed.GroupTarget(1));

        var decline = await client.PostWithTokenHeaderAsync(
            InviteUrl("DeclineInvite", ("inviteId", inviteId.ToString())),
            token);
        decline.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(decline, "Первый отказ не прошёл"));

        foreach (var action in new[] { "DeclineInvite", "AcceptInvite" })
        {
            var again = await client.PostWithTokenHeaderAsync(
                InviteUrl(action, ("inviteId", inviteId.ToString())),
                token);

            again.StatusCode.ShouldBe(
                HttpStatusCode.BadRequest,
                $"Повторный ответ ({action}) должен давать 400, а не падать 500-й");
            (await again.Content.ReadAsStringAsync()).ShouldNotBeNullOrWhiteSpace(
                $"Причина отказа ({action}) предназначена игроку, поэтому текст должен быть непустым");
        }
    }

    /// <summary>Создаёт приглашение и возвращает его идентификатор, взятый из списка полученных.</summary>
    private static async Task<AccommodationInviteIdentification> CreateInviteAsync(
        HttpClient client,
        string token,
        InviteSeed seed,
        int senderIndex,
        AccommodationGroupIdentification target)
    {
        var sender = seed.Claims[senderIndex];
        var targets = await GetTargetsAsync(client, sender);
        var found = FindTarget(targets, target);

        var create = await client.PostWithTokenHeaderAsync(
            InviteUrl("CreateInvite", ("claimId", sender.ToString()), ("target", found.TargetId.ToString())),
            token);
        create.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(create, "Приглашение не создалось"));

        // Идентификатор приглашения берётся из списка у получателя: наружу его не возвращает ни
        // ручка создания, ни сервис.
        var receiverClaimId = target.AsAccommodationRequestId() is { } requestId
            ? seed.ClaimByRequest(requestId)
            : target.AsClaimId()!;
        var incoming = await GetInvitesAsync(client, receiverClaimId, InviteDirection.Incoming);

        return incoming
            .Where(invite => invite.State == InviteState.Unanswered)
            .ToList()
            .ShouldHaveSingleItem()
            .InviteId;
    }

    /// <summary>Создаёт приглашение и сразу его принимает — подготовка состояния для теста.</summary>
    private static async Task InviteAndAcceptAsync(
        HttpClient client,
        string token,
        InviteSeed seed,
        int senderIndex,
        AccommodationGroupIdentification receiverTarget)
    {
        var inviteId = await CreateInviteAsync(client, token, seed, senderIndex, receiverTarget);

        var accept = await client.PostWithTokenHeaderAsync(
            InviteUrl("AcceptInvite", ("inviteId", inviteId.ToString())),
            token);
        accept.StatusCode.ShouldBe(HttpStatusCode.OK, await DescribeAsync(accept, "Приглашение не принялось"));
    }

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
    /// и на неутверждённых ручка целей вернула бы пустой список — приглашать было бы некого.
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
        foreach (var characterId in characters)
        {
            var (playerId, _) = await CreateUserAsync();
            claims.Add(await factory.Services.RunAsAsync(playerId, async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(projectId);
                return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                    characterId,
                    "Хочу играть эту роль",
                    FieldLayerContainer.Empty(projectInfo),
                    sensitiveDataAllowed: true);
            }));
        }

        var requestIds = await factory.Services.RunAsAsync(ownerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            foreach (var claimId in claims)
            {
                await claimService.ApproveByMaster(claimId, "Принято");
            }

            // Тип проживания выбран у всех, кроме последней заявки: она остаётся целью-заявкой.
            var result = new List<int>(claims.Count - 1);
            foreach (var claimId in claims.SkipLast(1))
            {
                AccommodationRequest request = await claimService.SetAccommodationType(
                    projectId.Value, claimId.ClaimId, roomTypeId.AccommodationTypeId);
                result.Add(request.Id);
            }

            return result;
        });

        return new InviteSeed(
            ownerId,
            ownerEmail,
            projectId,
            roomTypeId.AccommodationTypeId,
            [.. roomIds.Select(room => room.RoomId)],
            [.. claims.SkipLast(1)],
            requestIds,
            claims[^1]);
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
    private sealed record InviteSeed(
        UserIdentification OwnerId,
        string OwnerEmail,
        ProjectIdentification ProjectId,
        int RoomTypeId,
        IReadOnlyList<int> RoomIds,
        IReadOnlyList<ClaimIdentification> Claims,
        IReadOnlyList<int> RequestIds,
        ClaimIdentification ClaimWithoutRequest)
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

        /// <summary>Адрес заселения — ровно такой, какой собирает <c>rooms.js</c>.</summary>
        public string OccupyUrl(int roomId, params int[] requestIds)
            => $"{ProjectId.Value}/rooms/occupyroom?roomTypeId={RoomTypeId}&room={roomId}"
                + $"&reqId={string.Join(',', requestIds)}";

        public Task<HttpClient> CreateClientAsync(JoinApplicationFactory factory)
            => TestUserProjectHelpers.CreateAuthenticatedClientAsync(
                factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
                OwnerEmail,
                followsRedirects: false);

        /// <summary>
        /// Сколько человек живёт в комнате по данным страницы: атрибут <c>occupancy</c> у строки
        /// комнаты. Именно его читает <c>rooms.js</c>, поэтому проверять состояние по странице
        /// честнее, чем запросом в базу.
        /// </summary>
        public async Task<int> GetOccupancyAsync(HttpClient client, int roomId)
        {
            var response = await client.GetAsync(RoomTypeDetailsUrl);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Страница {RoomTypeDetailsUrl} не открылась");

            var document = await response.AsHtmlDocument();

            // HtmlAgilityPack приводит имена атрибутов к нижнему регистру.
            var row = document.DocumentNode.SelectSingleNode($"//tr[@roomid='{roomId}']")
                ?? throw new InvalidOperationException(
                    $"На странице {RoomTypeDetailsUrl} нет комнаты {roomId}");

            return row.GetAttributeValue("occupancy", -1);
        }
    }
}
