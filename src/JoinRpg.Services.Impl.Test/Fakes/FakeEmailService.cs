using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Записывает письма легаси-канала уведомлений (<see cref="IEmailService"/>) для проверки в тестах.
/// </summary>
/// <remarks>
/// Общего интерфейса у писем нет, поэтому копим по общему базовому классу
/// <see cref="EmailModelBase"/>: тест различает письма по конкретному типу.
/// </remarks>
/// <param name="journal">
/// Общий журнал обоих каналов рассылки, если тесту важен их взаимный порядок: ADR014 требует, чтобы
/// письма легаси-канала уходили строго после уведомлений.
/// </param>
internal sealed class FakeEmailService(List<object>? journal = null) : IEmailService
{
    public List<EmailModelBase> Sent { get; } = [];

    /// <summary>
    /// Вызывается на каждом письме. Нужен там, где важен не факт отправки, а момент: письма
    /// легаси-канала обязаны уходить уже после сохранения.
    /// </summary>
    public Action? OnEmail { get; set; }

    public Task Email(OccupyRoomEmail createClaimEmail) => Record(createClaimEmail);
    public Task Email(UnOccupyRoomEmail email) => Record(email);
    public Task Email(LeaveRoomEmail email) => Record(email);
    public Task Email(NewInviteEmail email) => Record(email);
    public Task Email(AcceptInviteEmail email) => Record(email);
    public Task Email(DeclineInviteEmail email) => Record(email);

    private Task Record(EmailModelBase email)
    {
        Sent.Add(email);
        journal?.Add(email);
        OnEmail?.Invoke();
        return Task.CompletedTask;
    }
}
