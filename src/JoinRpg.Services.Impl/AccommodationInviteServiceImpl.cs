using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Accommodation;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl;

/// <summary>
/// Приглашения к совместному проживанию. Каждая операция — мутация агрегата заявки действующей
/// стороны (ADR014): приглашение живёт на группе проживающих, а группа принадлежит заявке
/// (ADR018, §4).
/// </summary>
internal class AccommodationInviteServiceImpl(
    ICharacterPropsService characterPropsService,
    IAccommodationInviteRepository inviteRepository) : IAccommodationInviteService
{
    /// <inheritdoc />
    public Task CreateAccommodationInvite(
        ClaimIdentification senderClaimId,
        AccommodationRequestIdentification senderRequestId,
        AccommodationGroupIdentification target)
    {
        // TODO: Search for and reuse previously cancelled invitation(s) to the same person(s)

        _ = new IProjectEntityId[] { senderRequestId, target }.EnsureProject(senderClaimId.ProjectId);

        // Корень агрегата — заявка приглашающего (ADR014): приглашение живёт на её группе
        // проживающих, и доступ требуется ровно такой же, как у остальных операций с проживанием
        // (IClaimService.SetAccommodationType/LeaveAccommodationGroupAsync) — право расселять, а у
        // утверждённой заявки ещё и сам игрок с ответственным мастером.
        return characterPropsService.ChangeClaimAsync(
            senderClaimId,
            ClaimAccessRequirement.AccommodationChange,
            ProjectActiveRequirement.MustBeActive,
            target,
            async ctx =>
            {
                // Группа приглашающего берётся из его же заявки, а не по senderRequestId из
                // запроса: идентификатор приходит из формы и раньше проверялся только на
                // принадлежность проекту, то есть мастер мог пригласить соседа в чужую комнату.
                // Решается по снимкам (ADR022 §4): ссылка на группу — из CharacterClaimInfo, сама
                // группа — из плана поселения её типа.
                var sender = await ctx.LoadOwnAccommodationGroup();
                if (sender is { Group: var senderGroup } && senderGroup.Id != senderRequestId)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(senderClaimId.ProjectId,
                        "Заявка на проживание не принадлежит приглашающей заявке.");
                }

                var (receiverGroup, receiverClaimIds) = await LoadInviteTarget(ctx, target, sender?.Plan);

                EnsureCanInvite(
                    senderClaimId.ProjectId,
                    senderClaimId,
                    sender,
                    receiverGroup,
                    // Приглашается либо вся сложившаяся группа, либо один игрок, который группы
                    // ещё не имеет.
                    newDwellersCount: receiverGroup?.SubjectsCount ?? 1,
                    // Приглашение конкретной заявки адресовано ей одной, даже если у неё есть
                    // соседи; в приглашении группы такого «одного получателя» нет.
                    receiverClaimId: target.AsClaimId());

                foreach (var receiverClaimId in receiverClaimIds)
                {
                    ctx.AddEntity(new AccommodationInvite
                    {
                        ProjectId = senderClaimId.ProjectId.Value,
                        FromClaimId = senderClaimId.ClaimId,
                        ToClaimId = receiverClaimId.ClaimId,
                        IsAccepted = InviteState.Unanswered,
                    });
                }

                ctx.AddInviteNotification(new AccommodationInviteNotification(
                    receiverClaimIds,
                    ctx.CurrentUser.ToUserInfoHeader(),
                    InviteChangeKind.Created));
            });
    }

    /// <summary>
    /// Разворачивает цель приглашения: снимок её группы проживающих (если та уже есть) и заявки,
    /// которым уйдут приглашения.
    /// </summary>
    /// <param name="ctx">Контекст мутации заявки приглашающего.</param>
    /// <param name="target">Цель приглашения из запроса.</param>
    /// <param name="senderPlan">
    /// План поселения приглашающего, если тот уже загружен: цель того же типа берётся из него же,
    /// без второго запроса.
    /// </param>
    /// <remarks>
    /// Группа цели — снимок из плана поселения её <b>собственного</b> типа (ADR022 §3): она может
    /// оказаться другого типа, чем у приглашающего, то есть лежать в другом плане. Искать её в плане
    /// приглашающего значило бы получить <see cref="AccommodationGroupNotFoundException"/> вместо
    /// внятного «нужен такой же тип проживания».
    /// </remarks>
    private static async Task<(AccommodationGroupInfo? Group, IReadOnlyCollection<ClaimIdentification> Receivers)>
        LoadInviteTarget(
            ClaimMutationContext ctx,
            AccommodationGroupIdentification target,
            RoomCategoryPlan? senderPlan)
    {
        if (target.AsAccommodationRequestId() is { } receiverGroupId)
        {
            // Приглашается сложившаяся группа соседей — приглашение уходит каждому её участнику.
            var group = senderPlan?.Groups.FirstOrDefault(g => g.Id == receiverGroupId)
                ?? await LoadForeignGroup(ctx, receiverGroupId);

            return (group, group.Subjects);
        }

        if (target.AsClaimId() is { } receiverClaimId)
        {
            // Заявка приглашаемого грузится и ради проверки, что она вообще есть в этом проекте:
            // идентификатор цели приходит из запроса, и EnsureProject сверяет лишь объявленный в нём
            // проект. Без этого можно было пригласить заявку чужого проекта — приглашение создалось
            // бы, и игрок чужой игры получил бы уведомление.
            var (_, receiverTypeId, receiverGroupRef) = await ctx.LoadOtherClaimAccommodation(receiverClaimId);

            if (receiverTypeId is null)
            {
                // Тип не выбран — группы нет, приглашается одиночка.
                return (null, [receiverClaimId]);
            }

            var plan = await LoadPlanForType(ctx, receiverTypeId, senderPlan);
            return (plan.GetGroupOrDefault(receiverGroupRef), [receiverClaimId]);
        }

        //TODO[Localize]
        throw new AccommodationInviteNotAllowedException(target.ProjectId, "Не выбрано, кого приглашать.");
    }

    /// <summary>
    /// Снимок группы, которой нет в плане приглашающего: либо её нет вовсе, либо она другого типа.
    /// </summary>
    /// <remarks>
    /// По одному идентификатору группы её тип (а значит, и план) знает только её строка в БД —
    /// поэтому сначала трекаемая группа, и только ради существования и типа; решение дальше
    /// принимается по снимку из плана её типа.
    /// </remarks>
    private static async Task<AccommodationGroupInfo> LoadForeignGroup(
        ClaimMutationContext ctx,
        AccommodationRequestIdentification groupId)
    {
        var entity = await ctx.LoadAccommodationGroup(groupId)
            //TODO[Localize]
            ?? throw new AccommodationInviteNotAllowedException(groupId.ProjectId,
                "Приглашаемая группа проживающих не найдена.");

        var plan = await ctx.LoadRoomCategoryPlan(
            new AccommodationTypeIdentification(groupId.ProjectId, entity.AccommodationTypeId));
        return plan.GetGroup(groupId);
    }

    /// <summary>
    /// План поселения, из которого селится тип: уже загруженный, если тип селится из него, иначе
    /// новый запрос.
    /// </summary>
    private static async Task<RoomCategoryPlan> LoadPlanForType(
        ClaimMutationContext ctx,
        AccommodationTypeIdentification typeId,
        RoomCategoryPlan? knownPlan)
        => knownPlan is not null && knownPlan.AccommodationTypes.Any(type => type.Id == typeId)
            ? knownPlan
            : await ctx.LoadRoomCategoryPlan(typeId);

    /// <summary>
    /// Общие для обоих видов приглашения проверки. Раньше каждая из них молча возвращала
    /// <c>null</c>, и игрок не понимал, почему приглашение не отправилось.
    /// </summary>
    /// <param name="projectId">Проект — для исключения.</param>
    /// <param name="senderClaimId">Заявка приглашающего.</param>
    /// <param name="sender">
    /// Снимок группы приглашающего вместе с планом, из которого он взят
    /// (<see cref="ClaimMutationContext.LoadOwnAccommodationGroup"/>), либо <c>null</c>, если тип
    /// проживания не выбран.
    /// </param>
    /// <param name="receiverGroup">Снимок группы цели, либо <c>null</c>, если у цели группы нет.</param>
    /// <param name="newDwellersCount">Сколько человек переедет к приглашающему.</param>
    /// <param name="receiverClaimId">Заявка-цель, если приглашается заявка, а не группа.</param>
    /// <remarks>
    /// Решения — по доменным снимкам (ADR022 §4): вместимость и свободное место — из плана поселения,
    /// а не из навигаций трекаемых сущностей.
    /// </remarks>
    //TODO[Localize]
    internal static void EnsureCanInvite(
        ProjectIdentification projectId,
        ClaimIdentification senderClaimId,
        (RoomCategoryPlan Plan, AccommodationGroupInfo Group)? sender,
        AccommodationGroupInfo? receiverGroup,
        int newDwellersCount,
        ClaimIdentification? receiverClaimId = null)
    {
        // Нельзя пригласить самого себя: ни напрямую на свою заявку, ни в составе группового
        // приглашения на заявку на проживание, куда входит собственная заявка
        if (receiverClaimId == senderClaimId || receiverGroup?.Subjects.Contains(senderClaimId) == true)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "Нельзя пригласить самого себя.");
        }

        if (sender is not var (senderPlan, senderGroup))
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "У приглашающего не выбран тип проживания.");
        }

        // Приглашать и приглашаться могут только те, кого ещё не расселили по конкретным комнатам
        if (receiverGroup?.RoomId is not null || senderGroup.RoomId is not null)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "Нельзя приглашать, когда кто-то из участников уже расселён по комнатам.");
        }

        // Приглашать можно либо в такой же тип проживания, либо тех, кто ещё не выбрал тип
        if (receiverGroup is not null && receiverGroup.AccommodationTypeId != senderGroup.AccommodationTypeId)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "Приглашать можно только тех, кто выбрал такой же тип проживания.");
        }

        // Группа приглашающего не расселена (проверено выше), поэтому свободное место — вместимость
        // её типа минус состав: то же, что прежнее «состав + новые > вместимости».
        if (senderPlan.GetFreeSpaceForGroup(senderGroup.Id) < newDwellersCount)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "В номере не хватает мест для всех приглашаемых.");
        }
    }

    /// <inheritdoc />
    public async Task AcceptAccommodationInvite(AccommodationInviteIdentification inviteId)
    {
        // Стороны нужны до начала мутации: корень операции — заявка принимающего, и узнать её
        // можно только по приглашению. Само приглашение внутри перечитывается трекаемым.
        var participants = await inviteRepository.GetInviteParticipants(inviteId);

        await characterPropsService.ChangeClaimAsync(
            participants.Receiver,
            ClaimAccessRequirement.AccommodationChange,
            ProjectActiveRequirement.MustBeActive,
            inviteId,
            async ctx =>
            {
                var invite = await ctx.LoadInvite(inviteId);

                // Отвечать можно только на неотвеченное. Проверки не было, а принятые приглашения
                // из базы не удаляются и эндпойнт не идемпотентен — поэтому повторный приём (двойной
                // клик, повтор запроса) доходил до операции, у которой приглашающий и принимающий
                // уже живут в одной группе, и та группа отправлялась на удаление вместе с жильцами.
                if (invite.IsAccepted != InviteState.Unanswered)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(inviteId.ProjectId,
                        "На это приглашение уже ответили.");
                }

                // Решения — по доменным снимкам (ADR022 §4): группа приглашающего — по ссылке его
                // заявки и плану её типа, группа принимающего — по своему снимку. Заявка
                // приглашающего грузится одним запросом и вместе с трекаемой группой — та же
                // «ручка» для переезда.
                var (senderClaim, senderTypeId, senderGroupRef) =
                    await ctx.LoadOtherClaimAccommodation(participants.Sender);
                if (senderTypeId is null)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(inviteId.ProjectId,
                        "У приглашающего не выбран тип проживания.");
                }

                var senderPlan = await ctx.LoadRoomCategoryPlan(senderTypeId);
                var senderGroup = senderPlan.GetGroupOrDefault(senderGroupRef)
                    ?? throw new InvalidOperationException(
                        $"Claim {participants.Sender} has accommodation type {senderTypeId}, but no accommodation group");

                // Обе стороны уже соседи — переезжать некуда, а удалять «опустевшую» группу нельзя:
                // это та же группа, в которой все и живут. Нормальным потоком такое приглашение не
                // создаётся (EnsureCanInvite не даёт приглашать своих) и не остаётся висеть (приём
                // закрывает встречные приглашения, см. ResolveInvitesOfMovedClaim) — но в базе
                // лежат строки, созданные до этих правил.
                if (ctx.CharacterClaimInfo.AccommodationGroupId == senderGroupRef)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(inviteId.ProjectId,
                        "Приглашённый уже живёт вместе с приглашающим.");
                }

                // Переезжает вся группа принимающего, а если группы у него нет — он один. Его группа
                // может быть и другого типа (приём тип не сверяет), то есть лежать в другом плане.
                var receiverGroup = ctx.CharacterClaimInfo.AccommodationTypeId is { } receiverTypeId
                    ? (await LoadPlanForType(ctx, receiverTypeId, senderPlan))
                        .GetGroupOrDefault(ctx.CharacterClaimInfo.AccommodationGroupId)
                    : null;
                IReadOnlyCollection<ClaimIdentification> moving = receiverGroup?.Subjects ?? [ctx.CharacterClaimInfo.ClaimId];

                // Правило свободного места одно на систему — план поселения (ADR022 §3).
                if (senderPlan.GetFreeSpaceForGroup(senderGroup.Id) < moving.Count)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(inviteId.ProjectId,
                        "В номере не хватает мест для всех приглашаемых.");
                }

                // Трекаемые сущности — только «ручки» для записи, и обе уже на руках: группа
                // приглашающего приехала с его заявкой, группа принимающего вместе с составом — с
                // заявкой хэндла. Каждая сверяется со снимком, по которому принято решение.
                var trackedSenderGroup = senderClaim.AccommodationRequest;
                if (trackedSenderGroup is null || trackedSenderGroup.Id != senderGroup.Id.AccommodationRequestId)
                {
                    throw new InvalidOperationException(
                        $"Accommodation group {senderGroup.Id} of claim {participants.Sender} is not loaded with the claim");
                }

                var trackedReceiverGroup = ctx.Claim.AccommodationRequest;
                var movingClaims = receiverGroup is null
                    ? [ctx.Claim]
                    : TrackedSubjects(trackedReceiverGroup, receiverGroup);

                foreach (var claim in movingClaims)
                {
                    // Состав меняется внешним ключом (ADR022 §4); навигация — вместе с ним, чтобы EF6
                    // при DetectChanges видел согласованную пару, а не разбирал конфликт «ключ против
                    // ссылки». Коллекции Subjects не трогаем — их синхронизирует relationship fixup.
                    claim.AccommodationRequest_Id = trackedSenderGroup.Id;
                    claim.AccommodationRequest = trackedSenderGroup;
                }

                if (receiverGroup is not null)
                {
                    // Группа опустела — все её жильцы переехали. Порядок важен: DbSet.Remove сам
                    // вызывает DetectChanges, и к моменту удаления EF6 уже видит переехавших в новой
                    // группе. Удали мы группу раньше смены ссылок, EF6 по правилу опциональной связи
                    // обнулил бы внешний ключ её загруженным жильцам.
                    ctx.RemoveEntity(trackedReceiverGroup!);
                }

                // Итоговый состав группы — её снимок ДО переезда плюс переехавшие; только теперь
                // видно, кто кому стал соседом, — поэтому прочие приглашения разбираются после
                // переезда, а не внутри него.
                var neighbours = senderGroup.Subjects
                    .Concat(moving)
                    .Select(claimId => claimId.ClaimId)
                    .ToHashSet();
                foreach (var claimId in moving)
                {
                    await ResolveInvitesOfMovedClaim(ctx, claimId, invite, neighbours);
                }

                invite.IsAccepted = InviteState.Accepted;
                invite.ResolveDescription = ResolveDescription.Accepted;

                ctx.AddInviteNotification(new AccommodationInviteNotification(
                    [participants.Sender],
                    ctx.CurrentUser.ToUserInfoHeader(),
                    InviteChangeKind.Accepted));
            });
    }

    /// <summary>
    /// Трекаемые заявки группы принимающего — те самые, что переедут, по составу из снимка.
    /// </summary>
    /// <remarks>
    /// Берутся из состава трекаемой группы, загруженного вместе с заявкой хэндла, а не отдельной
    /// загрузкой каждой: тяжёлый запрос заявки на каждого соседа был бы N+1. Список материализуется
    /// до смены внешних ключей — иначе relationship fixup менял бы коллекцию под перебором. Если
    /// трекаемый состав разошёлся со снимком, решение о месте принято не про тех людей — это
    /// рассинхрон, а не пользовательская ошибка.
    /// </remarks>
    private static List<Claim> TrackedSubjects(
        AccommodationRequest? trackedGroup,
        AccommodationGroupInfo group)
    {
        if (trackedGroup is null || trackedGroup.Id != group.Id.AccommodationRequestId)
        {
            throw new InvalidOperationException(
                $"Accommodation group {group.Id} is not loaded with the claim");
        }

        var claims = trackedGroup.Subjects.ToList();
        var trackedIds = claims.Select(claim => claim.GetId()).ToHashSet();
        if (!trackedIds.SetEquals(group.Subjects))
        {
            throw new InvalidOperationException(
                $"Tracked subjects of accommodation group {group.Id} differ from its snapshot");
        }

        return claims;
    }

    /// <summary>
    /// Разбирает прочие приглашения заявки, которая только что переехала к новым соседям: ни одно
    /// из них не должно остаться неотвеченным.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Приглашение между теми, кто теперь живёт вместе, считается <b>сбывшимся</b>: его цель
    /// достигнута, отвечать на него нечего, и терминальный статус у него «принято автоматически».
    /// Так закрываются встречные приглашения — A пригласил B, B пригласил A, принято одно: второе
    /// иначе осталось бы висеть неотвеченным, и ответить на него было бы уже нельзя.
    /// </para>
    /// <para>
    /// Входящие приглашения <b>со стороны</b> отклоняются: игрок уже переехал. Исходящие наружу не
    /// трогаются — их снимает отказ или отзыв.
    /// </para>
    /// <para>
    /// До миграции разбор шёл только по соседям принявшего, но не по нему самому — его заявку
    /// убирали из группы раньше цикла, — поэтому игрок мог принять приглашение и тут же принять
    /// второе.
    /// </para>
    /// </remarks>
    private static async Task ResolveInvitesOfMovedClaim(
        ClaimMutationContext ctx,
        ClaimIdentification claimId,
        AccommodationInvite acceptedInvite,
        HashSet<int> neighbours)
    {
        var invites = await ctx.LoadInvitesForClaim(claimId);

        foreach (var invite in invites)
        {
            if (invite.Id == acceptedInvite.Id || invite.IsAccepted != InviteState.Unanswered)
            {
                continue;
            }

            var isIncoming = invite.ToClaimId == claimId.ClaimId;
            var counterparty = isIncoming ? invite.FromClaimId : invite.ToClaimId;

            if (neighbours.Contains(counterparty))
            {
                invite.IsAccepted = InviteState.Accepted;
                invite.ResolveDescription = ResolveDescription.AcceptedAuto;
            }
            else if (isIncoming)
            {
                invite.IsAccepted = InviteState.Declined;
                invite.ResolveDescription = ResolveDescription.DeclinedWithAcceptOther;
            }
        }
    }

    /// <inheritdoc />
    public Task DeclineAccommodationInvite(AccommodationInviteIdentification inviteId)
        // Отказывается приглашённый — значит корень операции его заявка.
        => ResolveInvite(inviteId, byReceiver: true, InviteState.Declined, ResolveDescription.Declined);

    /// <inheritdoc />
    public Task CancelAccommodationInvite(AccommodationInviteIdentification inviteId)
        // Отзывает приглашающий — корень операции его заявка.
        => ResolveInvite(inviteId, byReceiver: false, InviteState.Canceled, ResolveDescription.Canceled);

    /// <summary>
    /// Общее тело отказа и отзыва: меняется только сторона, чья заявка становится корнем операции,
    /// и то, в какое состояние уходит приглашение.
    /// </summary>
    /// <remarks>
    /// До миграции это был один метод с параметром <c>InviteState</c>, и из-за этого у обеих
    /// операций был один корень и одна проверка доступа — хотя отказ и отзыв делают разные люди.
    /// Неподдерживаемое состояние он принимал молча (<c>return null</c>); теперь такого параметра
    /// нет вовсе.
    /// </remarks>
    private async Task ResolveInvite(
        AccommodationInviteIdentification inviteId,
        bool byReceiver,
        InviteState newState,
        ResolveDescription resolveDescription)
    {
        var participants = await inviteRepository.GetInviteParticipants(inviteId);

        await characterPropsService.ChangeClaimAsync(
            byReceiver ? participants.Receiver : participants.Sender,
            ClaimAccessRequirement.AccommodationChange,
            ProjectActiveRequirement.MustBeActive,
            inviteId,
            async ctx =>
            {
                var invite = await ctx.LoadInvite(inviteId);

                // Отвечать можно только на неотвеченное — как и при приёме. Раньше состояние не
                // проверялось, и повторный отказ переписывал уже принятое приглашение.
                if (invite.IsAccepted != InviteState.Unanswered)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(inviteId.ProjectId,
                        "На это приглашение уже ответили.");
                }

                invite.IsAccepted = newState;
                invite.ResolveDescription = resolveDescription;

                // Уведомление уходит обеим сторонам, как и до миграции.
                ctx.AddInviteNotification(new AccommodationInviteNotification(
                    [participants.Sender, participants.Receiver],
                    ctx.CurrentUser.ToUserInfoHeader(),
                    InviteChangeKind.Cancelled));
            });
    }
}
