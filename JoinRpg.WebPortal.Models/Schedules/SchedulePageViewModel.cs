using System.Collections.Generic;

namespace JoinRpg.Web.Models.Schedules
{
    public class SchedulePageViewModel
    {
        public int ProjectId { get; set; }
        public string DisplayName { get; set; }

        public IReadOnlyList<TableHeaderViewModel> Rows { get; set; }
        public IReadOnlyCollection<TableHeaderViewModel> Columns { get; set; }

        public IReadOnlyCollection<ProgramItemViewModel> NotScheduledProgramItems { get; set; }
        public IReadOnlyCollection<ProgramItemViewModel> ConflictedProgramItems { get; set; }
        public IReadOnlyList<IReadOnlyList<ProgramItemViewModel>> Slots { get; set; }

        public IReadOnlyList<AppointmentViewModel> Appointments { get; set; }
        public IReadOnlyList<AppointmentViewModel> NotAllocated { get; set; }
        public IReadOnlyList<AppointmentViewModel> Intersections { get; set; }

        public int ColumnWidth { get; set; } = 200;
        public int LeftBarWidth { get; set; } = 100;
        public int RowHeight { get; set; } = 75;

        public int GridWidth => ColumnWidth * Columns.Count;
        public int GridHeight => RowHeight * Rows.Count;
    }
}
