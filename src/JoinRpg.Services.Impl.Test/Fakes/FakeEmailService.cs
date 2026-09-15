using JoinRpg.Services.Interfaces.Notification;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Записывает письма легаси-канала уведомлений (<see cref="IEmailService"/>) для проверки в тестах.
/// </summary>
/// <remarks>
/// Общего интерфейса у писем нет, поэтому копим по общему базовому классу
/// <see cref="EmailModelBase"/>: тест различает письма по конкретному типу.
/// </remarks>
internal sealed class FakeEmailService : IEmailService
{
    public List<EmailModelBase> Sent { get; } = [];

    public Task Email(OccupyRoomEmail createClaimEmail) => Record(createClaimEmail);
    public Task Email(UnOccupyRoomEmail email) => Record(email);
    public Task Email(LeaveRoomEmail email) => Record(email);
    public Task Email(NewInviteEmail email) => Record(email);
    public Task Email(AcceptInviteEmail email) => Record(email);
    public Task Email(DeclineInviteEmail email) => Record(email);

    private Task Record(EmailModelBase email)
    {
        Sent.Add(email);
        return Task.CompletedTask;
    }
}
