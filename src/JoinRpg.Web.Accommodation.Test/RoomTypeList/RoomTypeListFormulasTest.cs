using AngleSharp.Dom;
using Bunit;
using JoinRpg.Web.Accommodation.RoomTypeList;
using JoinRpg.Web.Models.Accommodation;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Accommodation.Test.RoomTypeList;

/// <summary>
/// Формулы счётчиков на странице «Поселение»: какие слагаемые выводятся, а какие опускаются.
/// </summary>
public class RoomTypeListFormulasTest : BunitContext
{
    /// <summary>Видимый текст формулы — без текста подсказок, которые лежат в DOM скрытыми до наведения.</summary>
    private static string Text(IRenderedComponent<Microsoft.AspNetCore.Components.IComponent> cut)
        => string.Join(" ", string.Concat(cut.Nodes.Select(VisibleText))
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string VisibleText(INode node) => node switch
    {
        IElement element when element.ClassList.Contains("join-tooltip") => "",
        IElement or IDocumentFragment => string.Concat(node.ChildNodes.Select(VisibleText)),
        _ => node.TextContent,
    };

    private IRenderedComponent<PendingRequestsFormula> RenderPendingComponent(int paid, int acceptedNotPaid)
        => Render<PendingRequestsFormula>(parameters => parameters
            .Add(c => c.Paid, paid)
            .Add(c => c.AcceptedNotPaid, acceptedNotPaid)
            .Add(c => c.Total, paid + acceptedNotPaid));

    private string RenderPending(int paid, int acceptedNotPaid) => Text(RenderPendingComponent(paid, acceptedNotPaid));

    [Theory]
    [InlineData(2, 1, "2 + 1 = 3")]
    [InlineData(0, 3, "3 = 3")]
    [InlineData(3, 0, "3 = 3")]
    [InlineData(0, 0, "")]
    public void PendingRequestsOmitZeroTerms(int paid, int acceptedNotPaid, string expected)
        => RenderPending(paid, acceptedNotPaid).ShouldBe(expected);

    /// <summary>
    /// Каждое число формулы объяснено своей подсказкой: по ним же цифры страницы находит
    /// интеграционный <c>AccommodationRoomTypeListScenario</c>.
    /// </summary>
    [Fact]
    public void PendingRequestsNumbersAreExplainedByTooltips()
    {
        var cut = RenderPendingComponent(paid: 2, acceptedNotPaid: 1);

        var tooltips = cut.FindAll(".join-tooltip-container")
            .ToDictionary(
                container => container.QuerySelector(".join-tooltip")!.TextContent.Trim(),
                container => container.FirstElementChild!.TextContent.Trim());

        tooltips.ShouldBe(new Dictionary<string, string>
        {
            [RoomTypeListItemViewModel.PaidTooltip] = "2",
            [RoomTypeListItemViewModel.AcceptedNotPaidTooltip] = "1",
            [RoomTypeListItemViewModel.TotalUnsettledTooltip] = "3",
        }, ignoreOrder: true);
    }

    private static RoomTypeOccupancySummary Occupancy(
        int poolOccupied = 0,
        int roomsCount = 3,
        int fullyFreeRoomsCount = 3,
        int fullyOccupiedRoomsCount = 0,
        int fullyOccupiedSeats = 0,
        int freeCapacity = 6)
        => new(
            Capacity: 2,
            Occupied: poolOccupied,
            PoolOccupied: poolOccupied,
            RoomsCount: roomsCount,
            ApprovedClaims: poolOccupied,
            FullyFreeRoomsCount: fullyFreeRoomsCount,
            FullyOccupiedRoomsCount: fullyOccupiedRoomsCount,
            FullyOccupiedSeats: fullyOccupiedSeats,
            FreeCapacity: freeCapacity,
            PaidCount: 0);

    private string RenderOccupied(RoomTypeOccupancySummary occupancy)
        => Text(Render<OccupiedSeatsFormula>(parameters => parameters.Add(c => c.Occupancy, occupancy)));

    private string RenderFree(RoomTypeOccupancySummary occupancy)
        => Text(Render<FreeSeatsFormula>(parameters => parameters.Add(c => c.Occupancy, occupancy)));

    [Fact]
    public void EmptyPoolShowsNoOccupiedAndAllFree()
    {
        var occupancy = Occupancy();

        RenderOccupied(occupancy).ShouldBe("Занято: нет");
        RenderFree(occupancy).ShouldBe("Свободно: 2 ✕ 3 = 6");
    }

    [Fact]
    public void PartiallyOccupiedRoomAddsPartialTerms()
    {
        // Три комнаты по 2 места: одна полная, одна с одним жильцом, одна пустая.
        var occupancy = Occupancy(
            poolOccupied: 3,
            fullyFreeRoomsCount: 1,
            fullyOccupiedRoomsCount: 1,
            fullyOccupiedSeats: 2,
            freeCapacity: 3);

        RenderOccupied(occupancy).ShouldBe("Занято: 2 + 1 = 3");
        RenderFree(occupancy).ShouldBe("Свободно: 2 ✕ 1 + 1 = 3");
    }

    [Fact]
    public void FullPoolShowsNoFree()
    {
        var occupancy = Occupancy(
            poolOccupied: 6,
            fullyFreeRoomsCount: 0,
            fullyOccupiedRoomsCount: 3,
            fullyOccupiedSeats: 6,
            freeCapacity: 0);

        RenderOccupied(occupancy).ShouldBe("Занято: 6 = 6");
        RenderFree(occupancy).ShouldBe("Свободно: нет");
    }
}
