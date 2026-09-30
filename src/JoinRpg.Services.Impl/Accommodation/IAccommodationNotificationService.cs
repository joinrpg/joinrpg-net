namespace JoinRpg.Services.Impl.Accommodation;

/// <summary>
/// Ставит в очередь уведомления о проживании (ADR003). Вызывать ПОСЛЕ сохранения операции: до него
/// операция ещё может упасть, и рассылка оказалась бы ложной (ADR018, §11).
/// </summary>
internal interface IAccommodationNotificationService
{
    /// <summary>
    /// Уведомление об изменении состава жителей комнаты.
    /// </summary>
    Task SendNotification(RoomOccupancyNotification model);
}
