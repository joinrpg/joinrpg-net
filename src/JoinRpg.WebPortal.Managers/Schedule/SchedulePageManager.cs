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

        var calendar = new Calendar();
        calendar.Events.AddRange(result.AllItems.Select(BuildIcalEvent));

        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(calendar) ?? ""; //TODO stream
    }

    private static CalendarEvent BuildIcalEvent(ProgramItemPlaced evt)
    {
        return new CalendarEvent
        {
            Start = ToCalDateTime(evt.StartTime, evt.ProjectTimeZone),
            End = ToCalDateTime(evt.EndTime, evt.ProjectTimeZone),
            Summary = evt.ProgramItem.Name,
            Location = string.Join(", ", evt.Rooms.Select(r => r.Name)),
            Description = evt.ProgramItem.Description.ToPlainTextWithoutHtmlEscape(),
        };
    }

    /// <summary>
    /// Время на часах в поясе проекта с его идентификатором. Не LocalDateTime — это было бы время сервера.
    /// </summary>
    internal static CalDateTime ToCalDateTime(DateTimeOffset time, TimeZoneInfo timeZone)
        => new(TimeZoneInfo.ConvertTime(time, timeZone).DateTime, timeZone.Id);

    private async Task<ScheduleResult> GetCompiledSchedule() => (await GetBuilder()).Build();

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
                    CharacterId = slot.Id!,
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
                    CharacterId = source.Id!,
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
                    CharacterId = source.Id!,
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
