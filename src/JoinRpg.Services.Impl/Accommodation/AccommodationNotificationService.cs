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
            projectInfo.AccommodationSettings.GetTypeById(model.AccommodationTypeId).Name,
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
        var header = textBuilder.GetHeader(data);
        var body = new NotificationEventTemplate(textBuilder.GetBody(data));

        // По уведомлению на заявку, а не одно на всех: ссылка ведёт на страницу заявки получателя,
        // где приглашениями и управляют, а она у каждого своя. Мастер, подписанный сразу на обе
        // стороны приглашения, получит два уведомления — легаси-канал в этом случае слал одно
        // письмо, но и ссылка в нём была верной не для всех.
        foreach (var claim in claims)
        {
            var args = new SubscribeCalculateArgs(
                // Не AccommodationChange: ответственный мастер на приглашения не подписан
                // (см. SubscribeCalculator.CreateForRespMaster) — легаси-канал уведомлял и его.
                Predicate: subscribe => subscribe.AccommodationInvitesChange,
                Initiator: model.Initiator,
                Player: [claim.Player],
                // Ответственный мастер на приглашения не подписан по определению, поэтому в расчёт
                // его не отдаём вовсе: предикат всё равно отсеет его запись, а
                // SubscribeCalculator.CreateForRespMaster по пути сделал бы Masters.Single(...) —
                // и упал бы на заявке, чей ответственный мастер уже снят с проекта.
                RespMasters: [],
                Claims: [claim.ClaimId],
                Characters: [claim.CharacterId],
                Finance: [],
                RespondingTo: []);

            var recipients = await subscribeCalculator.GetRecepients(args, projectInfo);
            if (recipients.Count == 0)
            {
                // Обычный случай: заявка самого инициатора. Он в получателях не значится, а
                // ответственный мастер на приглашения не подписан — уведомлять некого.
                continue;
            }

            await notificationService.QueueNotification(new NotificationEvent(
                NotificationClass.Accommodation,
                claim.ClaimId,
                header,
                body,
                recipients,
                model.Initiator.UserId));
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
