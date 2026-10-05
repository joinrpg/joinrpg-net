using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Schedules;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.Models;
using JoinRpg.Web.Schedule;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.WebPortal.Managers.Schedule;

public class SchedulePageManager(
    IProjectRepository project,
    ICurrentProjectAccessor currentProject,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository
        )
{
    public async Task<SchedulePageViewModel> GetSchedule()
    {
        var result = await GetCompiledSchedule();

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProject.ProjectId);
        var hasMasterAccess = projectInfo.HasMasterAccess(currentUserAccessor);
        var columns = result.Rooms.ToViewModel();
        var rows = result.TimeSlots.ToViewModel();
        var conflicted = result.Conflicted.ToViewModel(hasMasterAccess);
        var slots = result.Slots.Select2DList(x => x.ToViewModel(hasMasterAccess));

        MergeSlots(slots);

        return new SchedulePageViewModel()
        {
            ProjectId = currentProject.ProjectId,
            DisplayName = projectInfo.ProjectName,
            Columns = columns,
            Rows = rows,
            Appointments = BuildAppointments(slots, columns, rows, conflicted, hasMasterAccess),
            Intersections = BuildIntersections(conflicted, hasMasterAccess),
            NotAllocated = BuildNotAllocated(result.NotScheduled.ToViewModel(hasMasterAccess), columns, hasMasterAccess),
        };
    }

    //TODO we ignore acces rights here
    public async Task<string> GetIcalSchedule()
    {
        var result = await GetCompiledSchedule();

        var calendar = CreateCalendar([.. result.AllItems.Select(BuildIcalEvent)]);

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar) ?? ""; //TODO stream
    }

    /// <summary>
    /// Календарь с описанием (VTIMEZONE) каждого пояса, на который ссылаются события:
    /// без него не все клиенты (например, Outlook) понимают TZID с IANA-идентификатором.
    /// </summary>
    internal static Calendar CreateCalendar(IReadOnlyCollection<CalendarEvent> events)
    {
        var calendar = new Calendar();
        foreach (var zone in events.GroupBy(e => e.Start!.TzId!))
        {
            // Ical.Net описывает пояс от этой даты до «сейчас» и падает, если она позже «сейчас»
            var earliest = new[] { zone.Min(e => e.Start!.Value), DateTime.Now }.Min();
            _ = calendar.AddTimeZone(zone.Key, earliest, includeHistoricalData: false);
        }
        calendar.Events.AddRange(events);
        return calendar;
    }

    private static CalendarEvent BuildIcalEvent(ProgramItemPlaced evt)
    {
        return new CalendarEvent
        {
            // StartTime и EndTime уже в поясе проекта, DateTime — время на его часах
            Start = new CalDateTime(evt.StartTime.DateTime, evt.ProjectTimeZone.Id),
            End = new CalDateTime(evt.EndTime.DateTime, evt.ProjectTimeZone.Id),
            Summary = evt.ProgramItem.Name,
            Location = string.Join(", ", evt.Rooms.Select(r => r.Name)),
            Description = evt.ProgramItem.Description.ToPlainTextWithoutHtmlEscape(),
        };
    }

    private async Task<ScheduleResult> GetCompiledSchedule() => (await GetBuilder()).Build();

    private static List<AppointmentViewModel> BuildAppointments(
        List<List<ProgramItemViewModel>> slots,
        IReadOnlyCollection<TableHeaderViewModel> columns,
        IReadOnlyList<TableHeaderViewModel> rows,
        IReadOnlyCollection<ProgramItemViewModel> conflicted,
        bool hasMasterAccess)
    {
        var result = new List<AppointmentViewModel>(64);

        for (var i = 0; i < rows.Count; i++)
        {
            var row = slots[i];
            for (var j = 0; j < row.Count; j++)
            {
                var slot = row[j];
                if (slot.IsEmpty || slot.ColSpan == 0 || slot.RowSpan == 0)
                {
                    continue;
                }

                var rowIndex = i;
                var colIndex = j;

                var appointment = new AppointmentViewModel()
                {
                    ErrorType = conflicted.FirstOrDefault(pi => pi.Id == slot.Id) != null
                        ? AppointmentErrorType.Intersection
                        : null,
                    AllRooms = slot.ColSpan == columns.Count,
                    RoomIndex = colIndex,
                    RoomCount = slot.ColSpan,
                    HasMasterAccess = hasMasterAccess,
                    TimeSlotIndex = rowIndex,
                    TimeSlotsCount = slot.RowSpan,
                    DisplayName = slot.Name,
                    Description = slot.Description.ToHtmlString(),
                    CharacterId = slot.Id!,
                    Users = slot.Users,
                    Rooms = [.. columns
                        .SkipWhile((v, index) => index < colIndex)
                        .Take(slot.ColSpan)],
                    Slots = [.. rows
                        .SkipWhile((v, index) => index < rowIndex)
                        .Take(slot.RowSpan)]
                };
                result.Add(appointment);
            }
        }

        return result;
    }

    private static List<AppointmentViewModel> BuildIntersections(
        IReadOnlyCollection<ProgramItemViewModel> conflicted,
        bool hasMasterAccess)
        => conflicted
            .Select(
                source => new AppointmentViewModel()
                {
                    ErrorType = AppointmentErrorType.Intersection,
                    DisplayName = source.Name,
                    Description = source.Description.ToHtmlString(),
                    CharacterId = source.Id!,
                    Users = source.Users,
                    HasMasterAccess = hasMasterAccess,
                })
            .ToList();

    private static List<AppointmentViewModel> BuildNotAllocated(
        IReadOnlyCollection<ProgramItemViewModel> notScheduled,
        IReadOnlyCollection<TableHeaderViewModel> columns,
        bool hasMasterAccess)
        => notScheduled
            .Select(
                source => new AppointmentViewModel()
                {
                    ErrorType = AppointmentErrorType.NotLocated,
                    AllRooms = source.ColSpan == columns.Count,
                    DisplayName = source.Name,
                    Description = source.Description.ToHtmlString(),
                    CharacterId = source.Id!,
                    Users = source.Users,
                    HasMasterAccess = hasMasterAccess,
                })
            .ToList();

    private static void MergeSlots(List<List<ProgramItemViewModel>> slots)
    {
        for (var rowIndex = 0; rowIndex < slots.Count; rowIndex++)
        {
            var slotRow = slots[rowIndex];
            for (var colIndex = 0; colIndex < slotRow.Count; colIndex++)
            {
                var slot = slotRow[colIndex];
                if (slot.IsEmpty || slot.RowSpan == 0 || slot.ColSpan == 0)
                {
                    continue;
                }

                int CountSameSlots(IEnumerable<ProgramItemViewModel> sameSlots)
                {
                    return sameSlots.TakeWhile(s => s.Id == slot.Id).Count();
                }

                slot.ColSpan = CountSameSlots(slotRow.Skip(colIndex));

                slot.RowSpan = CountSameSlots(slots.Skip(rowIndex).Select(row => row[colIndex]));

                for (var i = 0; i < slot.RowSpan; i++)
                {
                    for (var j = 0; j < slot.ColSpan; j++)
                    {
                        if (i == 0 && j == 0)
                        {
                            continue;
                        }
                        var slotToRemove = slots[rowIndex + i][colIndex + j];
                        slotToRemove.ColSpan = 0;
                        slotToRemove.RowSpan = 0;
                    }
                }

            }
        }
    }

    /// <summary>
    /// Контролирует настройку конфигурации расписания
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyCollection<ScheduleConfigProblemsViewModel>> CheckScheduleConfiguration()
    {
        var project = await Project.GetProjectWithFieldsAsync(currentProject.ProjectId);
        ProjectInfo projectInfo;
        try
        {
            projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProject.ProjectId);
        }
        catch (InvalidOperationException)
        {
            return new[] { ScheduleConfigProblemsViewModel.ProjectNotFound };
        }

        if (project is null)
        {
            return new[] { ScheduleConfigProblemsViewModel.ProjectNotFound };
        }

        bool HasAccess(ProjectFieldInfo roomField)
        {
            if (roomField.IsPublic)
            {
                return true;
            }
            if (projectInfo.HasMasterAccess(currentUserAccessor))
            {
                return true;
            }
            if (currentUserAccessor.UserIdOrDefault is int userId && project.Claims.OfUserApproved(userId).Any())
            {
                return roomField.CanPlayerView;
            }
            return false;
        }

        IEnumerable<ScheduleConfigProblemsViewModel> Impl()
        {
            var roomField = projectInfo.RoomField;
            var timeSlotField = projectInfo.TimeSlotField;
            if (roomField is null || timeSlotField is null)
            {
                yield return ScheduleConfigProblemsViewModel.FieldsNotSet;
            }
            else
            {
                if (roomField.IsPublic != timeSlotField.IsPublic || roomField.CanPlayerView != timeSlotField.CanPlayerView)
                {
                    yield return ScheduleConfigProblemsViewModel.InconsistentVisibility;
                }

                if (!HasAccess(roomField))
                {
                    yield return ScheduleConfigProblemsViewModel.NoAccess;
                }

                // Сетку строят только из активных вариантов (см. ScheduleBuilder): если все
                // помещения или слоты удалены, показывать нечего, как и без вариантов вовсе.
                if (!timeSlotField.Variants.Any(v => v.IsActive))
                {
                    yield return ScheduleConfigProblemsViewModel.NoTimeSlots;
                }

                if (!roomField.Variants.Any(v => v.IsActive))
                {
                    yield return ScheduleConfigProblemsViewModel.NoRooms;
                }
            }
        }

        return Impl().ToList();
    }

    public async Task<ScheduleBuilder> GetBuilder()
    {
        var characters = await characterInfoRepository.GetAllCharacterInfos(
            currentProject.ProjectId,
            CharacterStatusSpec.Active);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProject.ProjectId);

        // Ведущие, указанные полем-ведущим (#4512), резолвятся одним запросом на всю сетку (ADR017 §4)
        var authorUsers = await userRepository.LoadUserLinks(ProgramItem.CollectAuthorUserIds(characters));

        return new ScheduleBuilder(characters, projectInfo, authorUsers);
    }

    private IProjectRepository Project { get; } = project;
}
