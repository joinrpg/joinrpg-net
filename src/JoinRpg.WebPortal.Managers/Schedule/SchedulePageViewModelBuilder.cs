using JoinRpg.Domain.Schedules;
using JoinRpg.Markdown;
using JoinRpg.Web.Models.Schedules;
using JoinRpg.Web.Models.UserProfile;
using JoinRpg.WebComponents;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.WebPortal.Managers.Schedule;

internal static class SchedulePageViewModelBuilder
{
    public static IReadOnlyList<ProgramItemViewModel> ToViewModel(this IEnumerable<ProgramItem> items, bool hasMasterAccess)
        => items.Select(item => item.ToViewModel(hasMasterAccess)).ToList();

    public static IReadOnlyList<TableHeaderViewModel> ToViewModel(this IEnumerable<ScheduleItemAttribute> items)
        => items.Select(item => item.ToViewModel()).ToList();

    public static ProgramItemViewModel ToViewModel(this ProgramItem? item, bool hasMasterAccess)
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
            Users = GetAuthors(item, hasMasterAccess),
        };
    }

    public static UserLinkViewModel[] GetAuthors(ProgramItem item, bool hasMasterAccess)
    {
        if (item.Authors.Length == 0)
        {
            return [];
        }
        if (!item.ShowAuthors && !hasMasterAccess)
        {
            return [UserLinkViewModel.Hidden];
        }
        return [.. item.Authors.Select(x => UserLinks.Create(x, ViewMode.Show))];
    }

    public static TableHeaderViewModel ToViewModel(this ScheduleItemAttribute scheduleItem)
    {
        if (scheduleItem is TimeSlot slot)
        {
            return new TableHeaderViewModel()
            {
                Id = slot.Id.ProjectFieldVariantId,
                Name = slot.Name,
                Description =
                    new MarkupString($"Начало: {slot.Options.StartTime:D}, Продолжительность: {slot.Options.TimeSlotInMinutes} минут<br /> {scheduleItem.Description.ToHtmlString().Value}"),
            };
        }
        if (scheduleItem is ScheduleRoom room)
        {
            return new TableHeaderViewModel()
            {
                Id = room.Id.ProjectFieldVariantId,
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
