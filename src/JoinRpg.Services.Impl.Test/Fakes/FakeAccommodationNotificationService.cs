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

    /// <summary>
    /// Вызывается на каждом уведомлении. Нужен там, где важен не факт отправки, а момент.
    /// </summary>
    public Action? OnNotification { get; set; }

    public Task SendNotification(RoomOccupancyNotification model)
    {
        RoomOccupancy.Add(model);
        journal?.Add(model);
        OnNotification?.Invoke();
        return Task.CompletedTask;
    }
}
