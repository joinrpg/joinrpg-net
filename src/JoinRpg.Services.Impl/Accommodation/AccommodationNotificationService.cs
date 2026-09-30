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
