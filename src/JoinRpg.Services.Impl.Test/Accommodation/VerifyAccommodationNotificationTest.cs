using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Impl.Accommodation;

namespace JoinRpg.Services.Impl.Test.Accommodation;

/// <summary>
/// Снапшоты всех текстов уведомлений о проживании (ADR003, «Тестирование»). До миграции тела этих
/// писем были закомментированы, то есть текста не видел никто — снапшот и есть его единственная
/// фиксация.
/// </summary>
public class VerifyAccommodationNotificationTest
{
    private readonly AccommodationNotificationTextBuilder builder = new();

    private static UserDisplayName Player(string name) => new(name, null);

    private static RoomOccupancyTextData Data(
        RoomOccupancyChangeKind kind,
        IReadOnlyCollection<UserDisplayName> changed,
        IReadOnlyCollection<UserDisplayName> remaining)
        => new(
            new ProjectName("Тестовая песочница"),
            AccommodationTypeName: "Палатка",
            RoomName: "101",
            Initiator: Player("Мастер Мастерович"),
            changed,
            remaining,
            kind);

    private Task VerifyText(RoomOccupancyTextData data, string caseName)
        => VerifyText(builder.GetHeader(data), builder.GetBody(data), caseName);

    /// <summary>Снапшот — это заголовок и тело вместе: пользователь видит их рядом.</summary>
    private static Task VerifyText(string header, string body, string caseName)
        => Verify($"{header}\n\n{body}").UseParameters(caseName);

    [Fact]
    public Task OccupiedEmptyRoom()
        => VerifyText(
            Data(RoomOccupancyChangeKind.Occupied, [Player("Вася"), Player("Петя")], []),
            nameof(OccupiedEmptyRoom));

    [Fact]
    public Task OccupiedRoomWithNeighbours()
        => VerifyText(
            Data(RoomOccupancyChangeKind.Occupied, [Player("Вася")], [Player("Маша"), Player("Даша")]),
            nameof(OccupiedRoomWithNeighbours));

    [Fact]
    public Task EvictedSomeInhabitants()
        => VerifyText(
            Data(RoomOccupancyChangeKind.Evicted, [Player("Вася")], [Player("Маша")]),
            nameof(EvictedSomeInhabitants));

    [Fact]
    public Task EvictedEverybody()
        => VerifyText(
            Data(RoomOccupancyChangeKind.Evicted, [Player("Вася"), Player("Петя")], []),
            nameof(EvictedEverybody));

    [Fact]
    public Task ClaimDeclinedWithNeighbours()
        => VerifyText(
            Data(RoomOccupancyChangeKind.ClaimDeclined, [Player("Вася")], [Player("Маша")]),
            nameof(ClaimDeclinedWithNeighbours));

    [Fact]
    public Task ClaimDeclinedAlone()
        => VerifyText(
            Data(RoomOccupancyChangeKind.ClaimDeclined, [Player("Вася")], []),
            nameof(ClaimDeclinedAlone));

    [Fact]
    public Task LeftRoomWithNeighbours()
        => VerifyText(
            Data(RoomOccupancyChangeKind.LeftRoom, [Player("Вася")], [Player("Маша")]),
            nameof(LeftRoomWithNeighbours));

    [Fact]
    public Task LeftRoomAlone()
        => VerifyText(
            Data(RoomOccupancyChangeKind.LeftRoom, [Player("Вася")], []),
            nameof(LeftRoomAlone));

    // Три [Fact] вместо [Theory]: InviteChangeKind — internal-тип, и публичный (для обнаружения
    // xUnit) метод его параметром принять не может.
    [Fact]
    public Task InviteCreated() => VerifyInvite(InviteChangeKind.Created, nameof(InviteCreated));

    [Fact]
    public Task InviteAccepted() => VerifyInvite(InviteChangeKind.Accepted, nameof(InviteAccepted));

    [Fact]
    public Task InviteCancelled() => VerifyInvite(InviteChangeKind.Cancelled, nameof(InviteCancelled));

    private Task VerifyInvite(InviteChangeKind kind, string caseName)
    {
        // Приглашаются игроки друг с другом, мастер в этом контуре обычно не участвует.
        var data = new InviteTextData(
            new ProjectName("Тестовая песочница"),
            Player("Вася"),
            kind);

        return VerifyText(builder.GetHeader(data), builder.GetBody(data), caseName);
    }
}
