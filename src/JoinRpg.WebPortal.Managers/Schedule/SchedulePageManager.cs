using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.Schedules;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.WebPortal.Managers.Schedule;

public class SchedulePageManager(
    IProjectRepository project,
    ICurrentProjectAccessor currentProject,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterRepository characterRepository,
    ILogger<SchedulePageManager> logger
        )
{
    public async Task<SchedulePageViewModel> GetSchedule()
    {
        var result = await GetCompiledSchedule();

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProject.ProjectId);
        var hasMasterAccess = projectInfo.HasMasterAccess(currentUserAccessor);
        var viewModel = new SchedulePageViewModel()
        {
            ProjectId = currentProject.ProjectId,
            DisplayName = projectInfo.ProjectName,
            NotScheduledProgramItems = result.NotScheduled.ToViewModel(hasMasterAccess),
            Columns = result.Rooms.ToViewModel(),
            Rows = result.TimeSlots.ToViewModel(),
            ConflictedProgramItems = result.Conflicted.ToViewModel(hasMasterAccess),
            Slots = result.Slots.Select2DList(x => x.ToViewModel(hasMasterAccess)),
        };

        MergeSlots(viewModel);
        await BuildAppointments(viewModel);

        return viewModel;
    }

    //TODO we ignore acces rights here
    public async Task<string> GetIcalSchedule()
    {
        var result = await GetCompiledSchedule();

        var timeZone = GuessTimeZone(result);

        if (timeZone == null)
        {
            return "";
        }
        var calendar = new Calendar();
        calendar.Events.AddRange(result.AllItems.Select(i => BuildIcalEvent(i, timeZone)));

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar) ?? ""; //TODO stream
    }

    private static readonly TimeZoneInfo mskTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Russian Standard Time");

    private TimeZoneInfo? GuessTimeZone(ScheduleResult result)
    {
        var offsets = result.AllItems.Select(i => i.StartTime.Offset).Distinct().ToList();

        if (offsets.Count == 0)
        {
            return null;
        }

        if (offsets.Count == 1 && offsets[0] == TimeSpan.FromHours(3))
        {
            return mskTimeZone;
        }

        if (offsets.Count == 1 && offsets[0] == TimeSpan.FromHours(0))
        {
            return TimeZoneInfo.Utc;
        }

        logger.LogError("Не удалось определить таймзону для проекта, встречаются варианты: {timeZoneHours}",
            string.Join(",", offsets));

        return null;
    }

    private CalendarEvent BuildIcalEvent(ProgramItemPlaced evt, TimeZoneInfo timeZone)
    {
        // TODO Здесь используется неявное предположение, что 
        return new CalendarEvent
        {
            Start = new CalDateTime(evt.StartTime.LocalDateTime, timeZone.Id),
            End = new CalDateTime(evt.EndTime.LocalDateTime, timeZone.Id),
            Summary = evt.ProgramItem.Name,
            Location = string.Join(", ", evt.Rooms.Select(r => r.Name)),
            Description = evt.ProgramItem.Description.ToPlainTextWithoutHtmlEscape(),
        };
    }

    private async Task<ScheduleResult> GetCompiledSchedule()
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProject.ProjectId);

        var characters = await characterRepository.LoadCharactersWithGroups(currentProject.ProjectId);
        var scheduleBuilder = new ScheduleBuilder(characters, projectInfo);
        return scheduleBuilder.Build();
    }

    private async Task BuildAppointments(SchedulePageViewModel viewModel)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProject.ProjectId);

        var hasMasterAccess = projectInfo.HasMasterAccess(currentUserAccessor);

        var result = new List<AppointmentViewModel>(64);

        for (var i = 0; i < viewModel.Rows.Count; i++)
        {
            var row = viewModel.Slots[i];
            for (var j = 0; j < row.Count; j++)
            {
                var slot = row[j];
                if (slot.IsEmpty || slot.ColSpan == 0 || slot.RowSpan == 0)
                {
                    continue;
                }

                var rowIndex = i;
                var colIndex = j;

                var appointment = new AppointmentViewModel(() => new Rect
                {
                    Left = colIndex * viewModel.ColumnWidth,
                    Top = rowIndex * viewModel.RowHeight,
                    Width = slot.ColSpan * viewModel.ColumnWidth,
                    Height = slot.RowSpan * viewModel.RowHeight
                })
                {
                    ErrorType = viewModel.ConflictedProgramItems.FirstOrDefault(pi => pi.Id == slot.Id) != null
                        ? AppointmentErrorType.Intersection
                        : null,
                    AllRooms = slot.ColSpan == viewModel.Columns.Count,
                    RoomIndex = colIndex,
                    RoomCount = slot.ColSpan,
                    HasMasterAccess = hasMasterAccess,
                    TimeSlotIndex = rowIndex,
                    TimeSlotsCount = slot.RowSpan,
                    DisplayName = slot.Name,
                    Description = slot.Description.ToHtmlString(),
                    ProjectId = slot.ProjectId,
                    CharacterId = slot.Id,
                    Users = slot.Users,
                    Rooms = [.. viewModel.Columns
                        .SkipWhile((v, index) => index < colIndex)
                        .Take(slot.ColSpan)],
                    Slots = [.. viewModel.Rows
                        .SkipWhile((v, index) => index < rowIndex)
                        .Take(slot.RowSpan)]
                };
                result.Add(appointment);
            }
        }

        viewModel.Appointments = result;

        viewModel.Intersections = viewModel.ConflictedProgramItems
            .Select(
                source => new AppointmentViewModel(
                    () => new Rect
                    {
                        Left = 0,
                        Top = 0,
                        Width = viewModel.ColumnWidth,
                        Height = viewModel.RowHeight
                    })
                {
                    ErrorMode = true,
                    ErrorType = AppointmentErrorType.Intersection,
                    DisplayName = source.Name,
                    Description = source.Description.ToHtmlString(),
                    ProjectId = source.ProjectId,
                    CharacterId = source.Id,
                    Users = source.Users,
                    HasMasterAccess = hasMasterAccess,
                })
            .ToList();

        viewModel.NotAllocated = viewModel.NotScheduledProgramItems
            .Select(
                source => new AppointmentViewModel(
                    () => new Rect
                    {
                        Left = 0,
                        Top = 0,
                        Width = viewModel.ColumnWidth,
                        Height = viewModel.RowHeight
                    })
                {
                    ErrorMode = true,
                    ErrorType = AppointmentErrorType.NotLocated,
                    AllRooms = source.ColSpan == viewModel.Columns.Count,
                    DisplayName = source.Name,
                    Description = source.Description.ToHtmlString(),
                    ProjectId = source.ProjectId,
                    CharacterId = source.Id,
                    Users = source.Users,
                    HasMasterAccess = hasMasterAccess,
                })
            .ToList();
    }

    private static void MergeSlots(SchedulePageViewModel viewModel)
    {
        for (var rowIndex = 0; rowIndex < viewModel.Slots.Count; rowIndex++)
        {
            var slotRow = viewModel.Slots[rowIndex];
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

                slot.RowSpan = CountSameSlots(viewModel.Slots.Skip(rowIndex).Select(row => row[colIndex]));

                for (var i = 0; i < slot.RowSpan; i++)
                {
                    for (var j = 0; j < slot.ColSpan; j++)
                    {
                        if (i == 0 && j == 0)
                        {
                            continue;
                        }
                        var slotToRemove = viewModel.Slots[rowIndex + i][colIndex + j];
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
            if (project.HasMasterAccess(currentUserAccessor))
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

                if (!timeSlotField.Variants.Any())
                {
                    yield return ScheduleConfigProblemsViewModel.NoTimeSlots;
                }

                if (!roomField.Variants.Any())
                {
                    yield return ScheduleConfigProblemsViewModel.NoRooms;
                }
            }
        }

        return Impl().ToList();
    }

    public async Task<ScheduleBuilder> GetBuilder()
    {
        var characters = await characterRepository.LoadCharactersWithGroups(currentProject.ProjectId);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(currentProject.ProjectId);

        return new ScheduleBuilder(characters, projectInfo);
    }

    private IProjectRepository Project { get; } = project;
}
