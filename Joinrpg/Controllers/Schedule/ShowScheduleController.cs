using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Schedules;
using JoinRpg.WebPortal.Managers.Schedule;

namespace JoinRpg.Web.Controllers.Schedule
{
    public class ShowScheduleController : Common.ControllerGameBase
    {
        public ShowScheduleController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IUserRepository userRepository, SchedulePageManager manager)
            : base(projectRepository, projectService, userRepository)
        {
            Manager = manager;
        }

        public SchedulePageManager Manager { get; }

        private ActionResult Error(int projectId, IEnumerable<ScheduleConfigProblemsViewModel> errors)
        {
            return View("..\\Payments\\Error", new ErrorViewModel
            {
                Message = "Ошибка открытия расписания",
                Description = string.Join(", ", errors.Select(e => e.GetDisplayName())),
                ReturnLink = Url.Action("Details", "Game", new { projectId }),
                ReturnText = "Страница проекта",
            });
        }

        [Authorize]
        public async Task<ActionResult> Index(int projectId)
        {
            var errors = await Manager.CheckScheduleConfiguration();
            if (errors.Any())
            {
                return Error(projectId, errors);
            }
            var schedule = await Manager.GetSchedule();
            return View(schedule);
        }
        
        [Authorize]
        public async Task<ActionResult> FullScreen(int projectId)
        {
            var errors = await Manager.CheckScheduleConfiguration();
            if (errors.Any())
            {
                return Error(projectId, errors);
            }
            var schedule = await Manager.GetSchedule();
            return View("FullScreen", schedule);
        }
    }
}
