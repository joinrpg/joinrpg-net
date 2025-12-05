using System.Text;
using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Schedules;
using JoinRpg.WebPortal.Managers.Schedule;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace JoinRpg.Portal.Controllers.Schedule;

[AllowAnonymous] // Access to schedule checked by scheduleManager
[Route("{projectId}/schedule")]
public class ShowScheduleController(
    IProjectRepository projectRepository,
    IProjectService projectService,
    SchedulePageManager manager) : Common.ControllerGameBase(projectRepository, projectService)
{
    private ViewResult Error(int projectId, IEnumerable<ScheduleConfigProblemsViewModel> errors)
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
        var errors = await manager.CheckScheduleConfiguration();
        if (errors.Any())
        {
            return Error(projectId, errors);
        }
        var schedule = await manager.GetSchedule();
        return View(schedule);
    }

    //TODO we ignore acces rights here
    [HttpGet("ical")]
    public async Task<ActionResult> Ical(int projectId)
    {
        var schedule = await manager.GetIcalSchedule();
        return File(Encoding.UTF8.GetBytes(schedule), "text/calendar");
    }

    [HttpGet("full")]
    public async Task<ActionResult> FullScreen(int projectId)
    {
        var errors = await manager.CheckScheduleConfiguration();
        if (errors.Any())
        {
            return Error(projectId, errors);
        }
        var schedule = await manager.GetSchedule();
        return View("FullScreen", schedule);
    }
}
