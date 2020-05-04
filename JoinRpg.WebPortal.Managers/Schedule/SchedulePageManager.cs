using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Schedules;
using JoinRpg.Interfaces;
using Joinrpg.Markdown;
using JoinRpg.Web.Models.Schedules;
using JoinRpg.WebPortal.Managers.Interfaces;

namespace JoinRpg.WebPortal.Managers.Schedule
{
    public class SchedulePageManager
    {
        public SchedulePageManager(
            IProjectRepository project,
            ICurrentProjectAccessor currentProject,
            ICurrentUserAccessor currentUserAccessor
            )
        {
            Project = project;
            CurrentProject = currentProject;
            CurrentUserAccessor = currentUserAccessor;
        }

        public async Task<SchedulePageViewModel> GetSchedule()
        {
            var project = await Project.GetProjectWithFieldsAsync(CurrentProject.ProjectId);
            var characters = await Project.GetCharacters(CurrentProject.ProjectId);
            var scheduleBuilder = new ScheduleBuilder(project, characters);
            var result = scheduleBuilder.Build();
            var viewModel = new SchedulePageViewModel()
            {
                ProjectId = CurrentProject.ProjectId,
                DisplayName = project.ProjectName,
                NotScheduledProgramItems = result.NotScheduled.ToViewModel(),
                Columns = result.Rooms.ToViewModel(),
                Rows = result.TimeSlots.ToViewModel(),
                ConflictedProgramItems = result.Conflicted.ToViewModel(),
                Slots = result.Slots.Select2DList(x => x.ToViewModel())
            };

            MergeSlots(viewModel);
            BuildAppointments(viewModel);

            return viewModel;
        }

        private void BuildAppointments(SchedulePageViewModel viewModel)
        {
            List<AppointmentViewModel> result = new List<AppointmentViewModel>(64);

            for (var i = 0; i < viewModel.Rows.Count; i++)
            {
                var row = viewModel.Slots[i];
                for (var j = 0; j < row.Count; j++)
                {
                    var slot = row[j];
                    if (slot.IsEmpty || slot.ColSpan == 0 || slot.RowSpan == 0)
                        continue;

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
                            : (AppointmentErrorType?)null,
                        AllRooms = slot.ColSpan == viewModel.Columns.Count,
                        RoomIndex = colIndex,
                        RoomCount = slot.ColSpan,
                        TimeSlotIndex = rowIndex,
                        TimeSlotsCount = slot.RowSpan,
                        DisplayName = slot.Name,
                        Description = slot.Description.ToHtmlString(),
                        ProjectId = slot.ProjectId,
                        CharacterId = slot.Id,
                        Users = slot.Users,
                    };
                    appointment.Rooms = viewModel.Columns
                        .SkipWhile((v, index) => index < colIndex)
                        .Take(slot.ColSpan)
                        .ToArray();
                    appointment.Slots = viewModel.Rows
                        .SkipWhile((v, index) => index < rowIndex)
                        .Take(slot.RowSpan)
                        .ToArray();
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
                        Users = source.Users
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
                        Users = source.Users
                    })
                .ToList();
        }

        private void MergeSlots(SchedulePageViewModel viewModel)
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
            var project = await Project.GetProjectWithFieldsAsync(CurrentProject.ProjectId);

            bool HasAccess(ProjectField roomField)
            {
                if (roomField.IsPublic)
                {
                    return true;
                }
                if (CurrentUserAccessor.UserIdOrDefault is null)
                {
                    return false;
                }
                if (project.HasMasterAccess(CurrentUserAccessor.UserId))
                {
                    return true;
                }
                if (project.Claims.OfUserApproved(CurrentUserAccessor.UserId).Any())
                {
                    return roomField.CanPlayerView;
                }
                return false;
            }

            IEnumerable<ScheduleConfigProblemsViewModel> Impl()
            {
                var settings = project.Details.ScheduleSettings;
                if (settings == null)
                {
                    yield return ScheduleConfigProblemsViewModel.FieldsNotSet;
                }
                else
                {
                    var roomField = settings.RoomField;
                    var timeSlotField = settings.TimeSlotField;
                    if (roomField.IsPublic != timeSlotField.IsPublic || roomField.CanPlayerView != timeSlotField.CanPlayerView)
                    {
                        yield return ScheduleConfigProblemsViewModel.InconsistentVisibility;
                    }

                    if (!HasAccess(roomField))
                    {
                        yield return ScheduleConfigProblemsViewModel.NoAccess;
                    }

                    if (!timeSlotField.DropdownValues.Any())
                    {
                        yield return ScheduleConfigProblemsViewModel.NoTimeSlots;
                    }

                    if (!roomField.DropdownValues.Any())
                    {
                        yield return ScheduleConfigProblemsViewModel.NoRooms;
                    }
                }
            }

            return Impl().ToList();
        }

        public IProjectRepository Project { get; }
        public ICurrentProjectAccessor CurrentProject { get; }
        public ICurrentUserAccessor CurrentUserAccessor { get; }
    }
}
