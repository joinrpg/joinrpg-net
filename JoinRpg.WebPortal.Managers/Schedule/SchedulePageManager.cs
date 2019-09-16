using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
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
            return new SchedulePageViewModel()
            {
                NotScheduledProgramItems = result.NotScheduled.ToViewModel(),
                Columns = result.Rooms.ToViewModel(),
                Rows = result.TimeSlots.ToViewModel(),
                ConflictedProgramItems = result.Conflicted.ToViewModel(),
                Slots = result.Slots.Select2DList(x => x.ToViewModel())
            };
        }

        public async Task<IEnumerable<ScheduleConfigProblemsViewModel>> CheckScheduleConfiguration()
        {
            var project = await Project.GetProjectWithFieldsAsync(CurrentProject.ProjectId);

            IEnumerable<ScheduleConfigProblemsViewModel> Impl()
            {
                var settings = project.Details.ProjectScheduleSettings;
                var roomField = settings.RoomField;
                var timeSlotField = settings.TimeSlotField;
                if (roomField == null || timeSlotField == null)
                {
                    yield return ScheduleConfigProblemsViewModel.FieldsNotSet;
                }
                else
                {
                    if (roomField.IsPublic != timeSlotField.IsPublic || roomField.CanPlayerView != timeSlotField.CanPlayerView)
                    {
                        yield return ScheduleConfigProblemsViewModel.InconsistentVisibility;
                    }

                    if (!project.HasMasterAccess(CurrentUserAccessor.UserId))
                    {
                        if (!roomField.CanPlayerView)
                        {
                            yield return ScheduleConfigProblemsViewModel.NoAccess;
                        }
                        else if (!roomField.IsPublic && !project.Claims.OfUserApproved(CurrentUserAccessor.UserId).Any())
                        {
                            yield return ScheduleConfigProblemsViewModel.NoAccess;
                        }
                    }
                }
            }

            return Impl();
        }

        public IProjectRepository Project { get; }
        public ICurrentProjectAccessor CurrentProject { get; }
        public ICurrentUserAccessor CurrentUserAccessor { get; }
    }
}
