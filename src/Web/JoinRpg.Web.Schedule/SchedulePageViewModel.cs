namespace JoinRpg.Web.Schedule;

public class SchedulePageViewModel
{
    public required ProjectIdentification ProjectId { get; set; }
    public string DisplayName { get; set; }

    public IReadOnlyList<TableHeaderViewModel> Rows { get; set; }
    public IReadOnlyCollection<TableHeaderViewModel> Columns { get; set; }

    public IReadOnlyList<AppointmentViewModel> Appointments { get; set; }
    public IReadOnlyList<AppointmentViewModel> NotAllocated { get; set; }
    public IReadOnlyList<AppointmentViewModel> Intersections { get; set; }
}
