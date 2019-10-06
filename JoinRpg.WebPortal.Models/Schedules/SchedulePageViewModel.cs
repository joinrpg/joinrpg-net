using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JoinRpg.Web.Models.Schedules
{
    public class SchedulePageViewModel
    {
        public IReadOnlyList<TableHeaderViewModel> Rows { get; set; }
        public IReadOnlyCollection<TableHeaderViewModel> Columns { get; set; }

        public IReadOnlyCollection<ProgramItemViewModel> NotScheduledProgramItems { get; set; }
        public IReadOnlyCollection<ProgramItemViewModel> ConflictedProgramItems { get; set; }
        public IReadOnlyList<IReadOnlyList<ProgramItemViewModel>> Slots { get; set; }
        
    }
}
