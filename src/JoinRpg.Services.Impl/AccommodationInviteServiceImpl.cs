using System.Data.Entity;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.ProjectMetadata.Accommodation;
using JoinRpg.Services.Impl.Accommodation;
using JoinRpg.Services.Impl.Characters;
using JoinRpg.Services.Impl.Claims;
using JoinRpg.Services.Impl.Projects;

namespace JoinRpg.Services.Impl;

/// <remarks>
/// Миграция на <c>ICharacterPropsService</c> (ADR014) идёт по одной операции: на props-сервис
/// переведено создание приглашения, остальные три метода ещё ходят в
/// <c>UnitOfWork.GetDbSet&lt;T&gt;()</c> через <c>[Obsolete] DbServiceImplBase</c>.
/// </remarks>
internal class AccommodationInviteServiceImpl : DbServiceImplBase, IAccommodationInviteService
{
    private readonly ICharacterPropsService characterPropsService;
    private readonly IAccommodationInviteRepository inviteRepository;

    public AccommodationInviteServiceImpl(
        IUnitOfWork unitOfWork,
        IAccommodationNotificationService notificationService,
        ICharacterPropsService characterPropsService,
        IAccommodationInviteRepository inviteRepository,
        ICurrentUserAccessor currentUserAccessor) :
        base(unitOfWork, currentUserAccessor)
    {
        NotificationService = notificationService;
        this.characterPropsService = characterPropsService;
        this.inviteRepository = inviteRepository;
    }

    private IAccommodationNotificationService NotificationService { get; }

    /// <inheritdoc />
    public Task CreateAccommodationInvite(
        ClaimIdentification senderClaimId,
        AccommodationRequestIdentification senderRequestId,
        AccommodationTargetIdentification target)
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
                var senderGroup = ctx.Claim.AccommodationRequest;
                if (senderGroup is not null && senderGroup.Id != senderRequestId.AccommodationRequestId)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(senderClaimId.ProjectId,
                        "Заявка на проживание не принадлежит приглашающей заявке.");
                }

                var (receiverGroup, receiverClaimIds) = await LoadInviteTarget(ctx, target);

                EnsureCanInvite(
                    senderClaimId.ProjectId,
                    ctx.ProjectInfo.AccommodationSettings,
                    senderClaimId,
                    senderGroup,
                    receiverGroup,
                    // Приглашается либо вся сложившаяся группа, либо один игрок, который группы
                    // ещё не имеет.
                    newDwellersCount: receiverGroup?.Subjects.Count ?? 1,
                    // Приглашение конкретной заявки адресовано ей одной, даже если у неё есть
                    // соседи; в приглашении группы такого «одного получателя» нет.
                    receiverClaimId: target.AsClaimId()?.ClaimId);

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
    /// Разворачивает цель приглашения: её группу проживающих (если та уже есть) и заявки, которым
    /// уйдут приглашения.
    /// </summary>
    /// <remarks>
    /// Обе стороны — выход за границу агрегата заявки, поэтому идут именованными загрузчиками
    /// контекста: так они трекаются тем же <c>DbContext</c>, через который пойдёт сохранение
    /// (ADR014).
    /// </remarks>
    private static async Task<(AccommodationRequest? Group, IReadOnlyCollection<ClaimIdentification> Receivers)>
        LoadInviteTarget(ClaimMutationContext ctx, AccommodationTargetIdentification target)
    {
        if (target.AsAccommodationRequestId() is { } receiverGroupId)
        {
            // Приглашается сложившаяся группа соседей — приглашение уходит каждому её участнику.
            var group = await ctx.LoadAccommodationGroup(receiverGroupId);
            if (group is null)
            {
                //TODO[Localize]
                throw new AccommodationInviteNotAllowedException(target.ProjectId,
                    "Приглашаемая группа проживающих не найдена.");
            }

            return (group, [.. group.Subjects.Select(claim => claim.GetId())]);
        }

        if (target.AsClaimId() is { } receiverClaimId)
        {
            // Заявка приглашаемого грузится не ради данных, а ради проверки, что она вообще есть в
            // этом проекте: идентификатор цели приходит из запроса, и EnsureProject сверяет лишь
            // объявленный в нём проект. Без этого можно было пригласить заявку чужого проекта —
            // приглашение создалось бы, и игрок чужой игры получил бы уведомление.
            _ = await ctx.LoadOtherClaim(receiverClaimId);

            return (await ctx.LoadAccommodationGroupForClaim(receiverClaimId), [receiverClaimId]);
        }

        //TODO[Localize]
        throw new AccommodationInviteNotAllowedException(target.ProjectId, "Не выбрано, кого приглашать.");
    }

    /// <summary>
    /// Общие для обоих видов приглашения проверки. Раньше каждая из них молча возвращала
    /// <c>null</c>, и игрок не понимал, почему приглашение не отправилось.
    /// </summary>
    //TODO[Localize]
    internal static void EnsureCanInvite(
        ProjectIdentification projectId,
        ProjectAccommodationSettings accommodationSettings,
        ClaimIdentification senderClaimId,
        AccommodationRequest? senderRequest,
        AccommodationRequest? receiverRequest,
        int newDwellersCount,
        int? receiverClaimId = null)
    {
        // Нельзя пригласить самого себя: ни напрямую на свою заявку, ни в составе группового
        // приглашения на заявку на проживание, куда входит собственная заявка
        if (receiverClaimId == senderClaimId.ClaimId
            || receiverRequest?.Subjects.Any(subject => subject.ClaimId == senderClaimId.ClaimId) == true)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "Нельзя пригласить самого себя.");
        }

        if (senderRequest is null)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "У приглашающего не выбран тип проживания.");
        }

        // Приглашать и приглашаться могут только те, кого ещё не расселили по конкретным комнатам
        if (receiverRequest?.AccommodationId != null || senderRequest.AccommodationId != null)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "Нельзя приглашать, когда кто-то из участников уже расселён по комнатам.");
        }

        // Приглашать можно либо в такой же тип проживания, либо тех, кто ещё не выбрал тип
        if (receiverRequest != null && receiverRequest.AccommodationTypeId != senderRequest.AccommodationTypeId)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "Приглашать можно только тех, кто выбрал такой же тип проживания.");
        }

        // Вместимость — из метаданных проекта (ADR015), а не из навигации senderRequest
        // .AccommodationType: та внутри мутации обернулась бы ленивой загрузкой.
        var capacity = accommodationSettings
            .GetTypeById(new AccommodationTypeIdentification(projectId, senderRequest.AccommodationTypeId))
            .Capacity;

        if (senderRequest.Subjects.Count + newDwellersCount > capacity)
        {
            throw new AccommodationInviteNotAllowedException(projectId,
                "В номере не хватает мест для всех приглашаемых.");
        }
    }

    /// <summary>
    /// Ставит уведомление о приглашении подписчикам перечисленных заявок. Инициатор берётся из
    /// текущего запроса — сущность пользователя из базы для этого не нужна.
    /// </summary>
    private Task NotifyAboutInvite(Claim[] recipients, InviteChangeKind kind)
        => NotificationService.SendNotification(new AccommodationInviteNotification(
            [.. recipients.Select(claim => claim.GetId())],
            currentUserAccessor.ToUserInfoHeader(),
            kind));

    /// <inheritdoc />
    public async Task<AccommodationInvite?> AcceptAccommodationInvite(AccommodationInviteIdentification inviteId)
    {
        // Стороны нужны до начала мутации: корень операции — заявка принимающего, и узнать её
        // можно только по приглашению. Само приглашение внутри перечитывается трекаемым.
        var participants = await inviteRepository.GetInviteParticipants(inviteId);

        return await characterPropsService.ChangeClaimAsync<AccommodationInviteIdentification, AccommodationInvite?>(
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

                var senderGroup = await ctx.LoadAccommodationGroupForClaim(participants.Sender);
                if (senderGroup is null)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(inviteId.ProjectId,
                        "У приглашающего не выбран тип проживания.");
                }

                // Переезжает вся группа принимающего, а если группы у него нет — он один.
                var receiverGroup = ctx.Claim.AccommodationRequest;

                // Обе стороны уже соседи — переезжать некуда, а удалять «опустевшую» группу нельзя:
                // это та же группа, в которой все и живут. Нормальным потоком такое приглашение не
                // создаётся (EnsureCanInvite не даёт приглашать своих) и не остаётся висеть (приём
                // закрывает встречные приглашения, см. ResolveInvitesOfMovedClaim) — но в базе
                // лежат строки, созданные до этих правил.
                if (ReferenceEquals(receiverGroup, senderGroup))
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(inviteId.ProjectId,
                        "Приглашённый уже живёт вместе с приглашающим.");
                }
                var moving = receiverGroup is null ? [ctx.Claim] : receiverGroup.Subjects.ToList();

                if (senderGroup.GetRoomFreeSpace(ctx.ProjectInfo) < moving.Count)
                {
                    //TODO[Localize]
                    throw new AccommodationInviteNotAllowedException(inviteId.ProjectId,
                        "В номере не хватает мест для всех приглашаемых.");
                }

                foreach (var claim in moving)
                {
                    _ = receiverGroup?.Subjects.Remove(claim);
                    senderGroup.Subjects.Add(claim);

                    // Обе стороны связи проставляются явно: до миграции это делалось только для
                    // принявшей заявки, а остальные переезжающие полагались на relationship fixup EF6.
                    claim.AccommodationRequest = senderGroup;
                    claim.AccommodationRequest_Id = senderGroup.Id;
                }

                if (receiverGroup is not null)
                {
                    // Группа опустела — все её жильцы переехали.
                    ctx.RemoveEntity(receiverGroup);
                }

                // Состав группы к этому моменту окончательный, и только теперь видно, кто кому стал
                // соседом, — поэтому прочие приглашения разбираются после переезда, а не внутри него.
                var neighbours = senderGroup.Subjects.Select(claim => claim.ClaimId).ToHashSet();
                foreach (var claim in moving)
                {
                    await ResolveInvitesOfMovedClaim(ctx, claim.GetId(), invite, neighbours);
                }

                invite.IsAccepted = InviteState.Accepted;
                invite.ResolveDescription = ResolveDescription.Accepted;

                ctx.AddInviteNotification(new AccommodationInviteNotification(
                    [participants.Sender],
                    ctx.CurrentUser.ToUserInfoHeader(),
                    InviteChangeKind.Accepted));

                return invite;
            });
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

    public async Task<AccommodationInvite?> CancelOrDeclineAccommodationInvite(
        AccommodationInviteIdentification inviteId,
        InviteState newState)
    {
        var acceptedStates = new[]
        {
            InviteState.Declined, InviteState.Canceled,
        };

        if (!acceptedStates.Contains(newState))
        {
            return null;
        }

        //todo: make null result descriptive
        var inviteRequest = await UnitOfWork.GetDbSet<AccommodationInvite>()
            .Where(invite => invite.Id == inviteId.AccommodationInviteId)
            .FirstOrDefaultAsync().ConfigureAwait(false);

        if (inviteRequest == null)
        {
            throw new Exception("Invite request not found.");
        }

        inviteRequest.IsAccepted = newState;
        inviteRequest.ResolveDescription = newState == InviteState.Canceled
            ? ResolveDescription.Canceled
            : ResolveDescription.Declined;
        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        var receivers = await UnitOfWork
            .GetDbSet<Claim>()
            .Where(claim =>
                claim.ClaimId == inviteRequest.FromClaimId ||
                claim.ClaimId == inviteRequest.ToClaimId)
            .ToArrayAsync()
            .ConfigureAwait(false);

        await NotifyAboutInvite(receivers, InviteChangeKind.Cancelled)
            .ConfigureAwait(false);

        return inviteRequest;
    }

    public async Task DeclineAllClaimInvites(ClaimIdentification claimId)
    {
        var inviteRequests = await UnitOfWork.GetDbSet<AccommodationInvite>()
            .Where(invite => invite.ToClaimId == claimId.ClaimId || invite.FromClaimId == claimId.ClaimId)
            .ToListAsync()
            .ConfigureAwait(false);

        if (inviteRequests.Count == 0)
        {
            return;
        }

        var claims = new List<int>();
        foreach (var accommodationInvite in inviteRequests)
        {
            claims.Add(accommodationInvite.FromClaimId);
            claims.Add(accommodationInvite.ToClaimId);
            accommodationInvite.IsAccepted = InviteState.Declined;
            accommodationInvite.ResolveDescription = ResolveDescription.ClaimCanceled;
        }

        await UnitOfWork.SaveChangesAsync().ConfigureAwait(false);

        claims = claims.Distinct().ToList();
        _ = claims.Remove(claimId.ClaimId);

        var receivers = await UnitOfWork
            .GetDbSet<Claim>()
            .Where(claim => claims.Contains(claim.ClaimId))
            .ToArrayAsync()
            .ConfigureAwait(false);

        // Проект больше не читаем: название уведомление возьмёт из метаданных по заявкам получателей.
        await NotifyAboutInvite(receivers, InviteChangeKind.Cancelled)
            .ConfigureAwait(false);
    }
}
