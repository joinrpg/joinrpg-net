namespace JoinRpg.Web.Schedule;

public enum AppointmentErrorType
{
    [Display(Name = "Не размещено в сетке расписания")]
    NotLocated,

    [Display(Name = "Пересечение с другими мероприятиями")]
    Intersection,
}

public class AppointmentBaseViewModel
{
    public string DisplayName { get; set; }
    public required CharacterIdentification CharacterId { get; set; }
    public required IReadOnlyCollection<UserLinkViewModel> Users { get; set; }
    public MarkupString Description { get; set; }
}

public class AppointmentViewModel : AppointmentBaseViewModel
{
    /// <summary>
    /// Место в сетке: первая комната и первый слот (с нуля) и сколько их занято.
    /// Для мероприятий вне сетки (пересечения, не размещённые) не заполняется.
    /// </summary>
    public int RoomIndex { get; set; }
    public int RoomCount { get; set; }
    public int TimeSlotIndex { get; set; }
    public int TimeSlotsCount { get; set; }

    public bool AllRooms { get; set; }
    public AppointmentErrorType? ErrorType { get; set; }

    public IReadOnlyCollection<TableHeaderViewModel> Rooms { get; set; } = Array.Empty<TableHeaderViewModel>();
    public IReadOnlyCollection<TableHeaderViewModel> Slots { get; set; } = Array.Empty<TableHeaderViewModel>();

    public bool HasMasterAccess { get; set; }
}
