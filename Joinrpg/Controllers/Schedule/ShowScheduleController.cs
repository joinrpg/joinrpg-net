using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
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

        public async Task<ActionResult> IndexAsync(int projectId)
        {
            var check = await Manager.CheckScheduleConfiguration();
            if (check.Any())
            {
                return View("ScheduleFailed", check);
            }
            var schedule = await Manager.GetSchedule();
            return View(schedule);
        }
    }
}
