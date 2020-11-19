using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.Markdown;
using JoinRpg.Domain.Schedules;
using JoinRpg.Helpers.Web;
using JoinRpg.Web.Models.Schedules;

namespace JoinRpg.WebPortal.Managers.Schedule
{
    internal static class SchedulePageViewModelBuilder
    {
        public static IReadOnlyList<ProgramItemViewModel> ToViewModel(this IEnumerable<ProgramItem> items)
            => items.Select(item => item.ToViewModel()).ToList();

        public static IReadOnlyList<TableHeaderViewModel> ToViewModel(this IEnumerable<ScheduleItemAttribute> items)
            => items.Select(item => item.ToViewModel()).ToList();

        public static ProgramItemViewModel ToViewModel(this ProgramItem item)
        {
            if (item == null)
            {
                return ProgramItemViewModel.Empty;
            }
            return new ProgramItemViewModel()
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                ProjectId = item.ProjectId,
                Users = item.Authors,
            };
        }

        public static TableHeaderViewModel ToViewModel(this ScheduleItemAttribute scheduleItem)
        {
            if (scheduleItem is TimeSlot slot)
            {
                return new TableHeaderViewModel()
                {
                    Id = slot.Id,
                    Name = slot.Name,
                    Description =
                        $"Начало: {slot.Options.StartTime:D}, Продолжительность: {slot.Options.TimeSlotInMinutes} минут<br />"
                            .MarkAsHtmlString() + scheduleItem.Description.ToHtmlString(),
                };
            }
            if (scheduleItem is ScheduleRoom room)
            {
                return new TableHeaderViewModel()
                {
                    Id = room.Id,
                    Name = room.Name,
                    Description = room.Description.ToHtmlString()
                };
            }
            throw new NotImplementedException();
        }

        // Inspired by https://stackoverflow.com/a/48290430/408666
        public static List<List<R>> Select2DList<T, R>(this List<List<T>> items, Func<T, R> action)
        {
            var list = new List<List<R>>(items.Count);
            foreach (var row in items)
            {
                var projection = new List<R>(row.Count);
                foreach (var x in row)
                {
                    projection.Add(action(x));
                }
                list.Add(projection);
            }
            return list;
        }
    }
}
