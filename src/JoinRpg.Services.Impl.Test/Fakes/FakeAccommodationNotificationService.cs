using JoinRpg.Services.Impl.Accommodation;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Записывает уведомления о проживании вместо реальной постановки в очередь.
/// </summary>
/// <param name="journal">
/// Общий журнал, если тесту важен взаимный порядок событий: уведомления обязаны уходить строго
/// после сохранения (ADR018, §11), а в контуре заявок — ещё и после уведомлений по комментариям
/// (ADR014).
/// </param>
internal sealed class FakeAccommodationNotificationService(List<object>? journal = null)
    : IAccommodationNotificationService
{
    public List<RoomOccupancyNotification> RoomOccupancy { get; } = [];

    public List<AccommodationInviteNotification> Invites { get; } = [];

    /// <summary>
    /// Вызывается на каждом уведомлении. Нужен там, где важен не факт отправки, а момент.
    /// </summary>
    public Action? OnNotification { get; set; }

    public Task SendNotification(RoomOccupancyNotification model)
    {
        RoomOccupancy.Add(model);
        return Record(model);
    }

    public Task SendNotification(AccommodationInviteNotification model)
    {
        Invites.Add(model);
        return Record(model);
    }

    private Task Record(object model)
    {
        journal?.Add(model);
        OnNotification?.Invoke();
        return Task.CompletedTask;
    }
}
