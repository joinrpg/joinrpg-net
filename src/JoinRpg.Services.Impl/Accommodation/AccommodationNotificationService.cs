using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces.Notifications;
using JoinRpg.Services.Impl.Claims;

namespace JoinRpg.Services.Impl.Accommodation;

/// <summary>
/// Реализация <see cref="IAccommodationNotificationService"/> поверх общего сервиса уведомлений
/// (ADR003): получатели считаются тем же <see cref="SubscribeCalculator"/>, что и у заявок,
/// текст — <see cref="AccommodationNotificationTextBuilder"/>.
/// </summary>
internal class AccommodationNotificationService(
    INotificationService notificationService,
    IProjectMetadataRepository projectMetadataRepository,
    IClaimsRepository claimsRepository,
    SubscribeCalculator subscribeCalculator,
    AccommodationNotificationTextBuilder textBuilder
    ) : IAccommodationNotificationService
{
    public async Task SendNotification(RoomOccupancyNotification model)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(model.RoomId.ProjectId);

        // Заголовки заявок нужны и на текст (имена игроков), и на получателей (игрок,
        // ответственный мастер, персонаж — от него считаются подписки по дереву групп).
        var claims = await claimsRepository.GetClaimHeadersWithPlayer([.. model.Changed, .. model.Remaining]);
        if (claims.Count == 0)
        {
            // Некому ни писать, ни о ком писать. Ранний выход обязателен: репозитории подписок и
            // персонажей на пустом списке идентификаторов падают (First()/ToIntListSameProject()),
            // а мы здесь уже ПОСЛЕ SaveChanges — исключение вернуло бы 500 на выполненной операции.
            // Достижимо на группе поселения без заявок: план такие не отфильтровывает.
            return;
        }

        var byId = claims.ToDictionary(claim => claim.ClaimId);

        var data = new RoomOccupancyTextData(
            projectInfo.ProjectName,
            projectInfo.AccommodationSettings.GetRoomCategoryById(model.RoomCategoryId).Name,
            model.RoomName,
            model.Initiator.DisplayName,
            PlayerNames(model.Changed, byId),
            PlayerNames(model.Remaining, byId),
            model.Kind);

        // Получают уведомление подписчики всех, кто живёт в комнате после операции, плюс сами
        // сдвинутые — так же, как считал легаси-канал.
        var args = new SubscribeCalculateArgs(
            Predicate: subscribe => subscribe.AccommodationChange,
            Initiator: model.Initiator,
            Player: [.. claims.Select(claim => claim.Player)],
            RespMasters: [.. claims.Select(claim => (UserIdentification?)claim.ResponsibleMasterUserId)],
            Claims: [.. claims.Select(claim => claim.ClaimId)],
            Characters: [.. claims.Select(claim => (CharacterIdentification?)claim.CharacterId)],
            Finance: [],
            RespondingTo: []);

        await notificationService.QueueNotification(new NotificationEvent(
            NotificationClass.Accommodation,
            model.RoomId,
            textBuilder.GetHeader(data),
            new NotificationEventTemplate(textBuilder.GetBody(data)),
            await subscribeCalculator.GetRecepients(args, projectInfo),
            model.Initiator.UserId));
    }

    public async Task SendNotification(AccommodationInviteNotification model)
    {
        var claims = await claimsRepository.GetClaimHeadersWithPlayer(model.RecipientClaims);
        if (claims.Count == 0)
        {
            return;
        }

        // Проект — из самих заявок: все получатели одной операции живут в одном проекте, а второму
        // источнику неоткуда разойтись с первым.
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claims.First().ClaimId.ProjectId);

        var data = new InviteTextData(projectInfo.ProjectName, model.Initiator.DisplayName, model.Kind);

        await QueueToPlayersAndOnceToSubscribers(
            claims,
            model.Host,
            model.Initiator,
            textBuilder.GetHeader(data),
            new NotificationEventTemplate(textBuilder.GetBody(data)),
            projectInfo);
    }

    public async Task SendNotification(AccommodationGroupMergedNotification model)
    {
        var claims = await claimsRepository.GetClaimHeadersWithPlayer(model.Members);
        if (claims.Count == 0)
        {
            return;
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claims.First().ClaimId.ProjectId);
        var byId = claims.ToDictionary(claim => claim.ClaimId);

        var data = new GroupMergedTextData(
            projectInfo.ProjectName,
            model.Initiator.DisplayName,
            PlayerNames(model.Members, byId));

        // Подписка — та же, что у приглашений: объединение заменяет пару «пригласить — принять», и
        // получать его должны те же люди.
        await QueueToPlayersAndOnceToSubscribers(
            claims,
            model.Host,
            model.Initiator,
            textBuilder.GetHeader(data),
            new NotificationEventTemplate(textBuilder.GetBody(data)),
            projectInfo);
    }

    /// <summary>
    /// Рассылка подписчикам приглашений (<see cref="SubscriptionOptions.AccommodationInvitesChange"/>)
    /// по набору заявок. Игрокам — по уведомлению со ссылкой на свою заявку: чужую заявку игрок не
    /// откроет. Мастерам и прочим подписчикам — одно на всех со ссылкой на <paramref name="host"/>:
    /// подписанный на несколько заявок иначе получил бы столько одинаковых писем, сколько их задето.
    /// </summary>
    private async Task QueueToPlayersAndOnceToSubscribers(
        IReadOnlyCollection<ClaimWithPlayer> claims,
        ClaimIdentification host,
        UserInfoHeader initiator,
        string header,
        NotificationEventTemplate body,
        ProjectInfo projectInfo)
    {
        var players = new HashSet<UserIdentification>();
        var masters = new Dictionary<UserIdentification, NotificationRecepient>();
        foreach (var claim in claims)
        {
            var args = new SubscribeCalculateArgs(
                // Не AccommodationChange: ответственный мастер на приглашения не подписан
                // (см. SubscribeCalculator.CreateForRespMaster) — легаси-канал уведомлял и его.
                Predicate: subscribe => subscribe.AccommodationInvitesChange,
                Initiator: initiator,
                Player: [claim.Player],
                // Ответственный мастер на приглашения не подписан по определению, поэтому в расчёт
                // его не отдаём вовсе: предикат всё равно отсеет его запись.
                RespMasters: [],
                Claims: [claim.ClaimId],
                Characters: [claim.CharacterId],
                Finance: [],
                RespondingTo: []);

            var recipients = await subscribeCalculator.GetRecepients(args, projectInfo);
            var player = recipients.Where(r => r.SubscriptionReason == SubscriptionReason.Player).ToList();
            foreach (var recipient in recipients.Except(player))
            {
                _ = masters.TryAdd(recipient.UserId, recipient);
            }

            if (player.Count == 0)
            {
                // Обычно — игрок сам инициатор.
                continue;
            }

            players.UnionWith(player.Select(r => r.UserId));
            await notificationService.QueueNotification(new NotificationEvent(
                NotificationClass.Accommodation,
                claim.ClaimId,
                header,
                body,
                player,
                initiator.UserId));
        }

        // Игрок одной из заявок, подписанный ещё и как мастер, своё уже получил.
        var masterRecipients = masters.Values.Where(r => !players.Contains(r.UserId)).ToList();
        if (masterRecipients.Count > 0)
        {
            await notificationService.QueueNotification(new NotificationEvent(
                NotificationClass.Accommodation,
                host,
                header,
                body,
                masterRecipients,
                initiator.UserId));
        }
    }

    /// <summary>
    /// Имена игроков строго в порядке переданных заявок: порядок виден в тексте уведомления, а
    /// репозиторий его не обещает. Заявку, которой в выборке не оказалось, молча пропускаем — на
    /// такой ситуации уведомление ронять незачем.
    /// </summary>
    private static IReadOnlyCollection<UserDisplayName> PlayerNames(
        IReadOnlyCollection<ClaimIdentification> claimIds,
        Dictionary<ClaimIdentification, ClaimWithPlayer> byId)
        => [.. claimIds
            .Select(claimId => byId.TryGetValue(claimId, out var claim) ? claim : null)
            .WhereNotNull()
            .Select(claim => claim.Player.DisplayName)];
}
