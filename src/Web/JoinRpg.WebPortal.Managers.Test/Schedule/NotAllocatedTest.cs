using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Schedules;
using JoinRpg.Web.Schedule;
using JoinRpg.WebPortal.Managers.Schedule;

namespace JoinRpg.WebPortal.Managers.Test.Schedule;

/// <summary>
/// Карточки мероприятий, не размещённых в сетке (#5754): в них видно, что мастер уже выбрал.
/// </summary>
public class NotAllocatedTest
{
    private readonly MockedProject mock = new();

    private ProjectFieldVariant MakeVariant(int fieldId, int variantId, string label, bool isActive = true)
        => new(
            new ProjectFieldVariantIdentification(new ProjectFieldIdentification(mock.ProjectInfo.ProjectId, fieldId), variantId),
            label,
            Price: 0,
            IsPlayerSelectable: true,
            IsActive: isActive,
            CharacterGroupId: null,
            Description: null,
            MasterDescription: null,
            ProgrammaticValue: null,
            WasEverUsed: true,
            ParentFieldName: "Поле");

    private ProgramItemNotScheduled MakeItem(ProjectFieldVariant[] rooms, ProjectFieldVariant[] timeSlots)
        => new(
            new ProgramItem(mock.GetCharacterInfo(mock.Character), new Dictionary<UserIdentification, UserInfoHeader>()),
            rooms,
            timeSlots);

    private static AppointmentViewModel Build(ProgramItemNotScheduled item, int roomCount)
        => SchedulePageManager.BuildNotAllocated([item], roomCount, hasMasterAccess: true).ShouldHaveSingleItem();

    [Fact]
    public void SelectedRoomWithoutTimeSlot_IsShown()
    {
        var appointment = Build(MakeItem([MakeVariant(1, 1, "Зал")], []), roomCount: 2);

        appointment.ErrorType.ShouldBe(AppointmentErrorType.NotLocated);
        appointment.Rooms.Select(r => r.Name).ShouldBe(["Зал"]);
        appointment.Slots.ShouldBeEmpty();
        appointment.AllRooms.ShouldBeFalse();
    }

    [Fact]
    public void SelectedTimeSlotWithoutRoom_IsShown()
    {
        var appointment = Build(MakeItem([], [MakeVariant(2, 1, "Утро")]), roomCount: 2);

        appointment.Rooms.ShouldBeEmpty();
        appointment.Slots.Select(s => s.Name).ShouldBe(["Утро"]);
        appointment.AllRooms.ShouldBeFalse();
    }

    [Fact]
    public void DeletedVariant_IsMarked()
    {
        var appointment = Build(
            MakeItem([MakeVariant(1, 1, "Зал"), MakeVariant(1, 2, "Подвал", isActive: false)], []),
            roomCount: 1);

        appointment.Rooms.Select(r => r.Name).ShouldBe(["Зал", "Подвал (удалён)"]);
        // Все живые помещения выбраны, но «все» скрыло бы удалённое
        appointment.AllRooms.ShouldBeFalse();
    }

    [Fact]
    public void AllRoomsSelected_IsAllRooms()
    {
        var appointment = Build(MakeItem([MakeVariant(1, 1, "Зал"), MakeVariant(1, 2, "Холл")], []), roomCount: 2);

        appointment.AllRooms.ShouldBeTrue();
    }
}
