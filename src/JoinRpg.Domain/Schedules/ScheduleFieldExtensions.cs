using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.Schedules
{
    public static class ScheduleFieldExtensions
    {
        public static ProjectField GetTimeSlotFieldOrDefault(this Project project) => project.ProjectFields.SingleOrDefault(f => f.FieldType == ProjectFieldType.ScheduleTimeSlotField);
        public static ProjectField GetRoomFieldOrDefault(this Project project) => project.ProjectFields.SingleOrDefault(f => f.FieldType == ProjectFieldType.ScheduleRoomField);
    }
}
