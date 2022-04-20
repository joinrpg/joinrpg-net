using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Schedules;
using JoinRpg.WebPortal.Managers.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace JoinRpg.Portal.Controllers.Schedule
{
    [AllowAnonymous] // Access to schedule checked by scheduleManager
    [Route("{projectId}/schedule")]
    public class ShowScheduleController : Common.ControllerGameBase
    {
        public ShowScheduleController(
            IProjectRepository projectRepository,
            IProjectService projectService,
            IUserRepository userRepository, SchedulePageManager manager)
            : base(projectRepository, projectService, userRepository) => Manager = manager;

        public SchedulePageManager Manager { get; }

        private ActionResult Error(int projectId, IEnumerable<ScheduleConfigProblemsViewModel> errors)
        {
            return View("Error", new ErrorViewModel
            {
                Message = "Ошибка открытия расписания",
                Description = string.Join(", ", errors.Select(e => e.GetDisplayName())),
                ReturnLink = Url.Action(new UrlActionContext()
                {
                    Action = "Details",
                    Controller = "Game",
                    Values = new { projectId },
                }),
                ReturnText = "Страница проекта",
            });
        }

        [HttpGet("")]
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

        [HttpGet("full")]
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
