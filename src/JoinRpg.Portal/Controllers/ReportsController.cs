using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models.Reports;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[MasterAuthorize()]
[Route("{projectId}/reports")]
public class ReportsController : ControllerGameBase
{
    [HttpGet("2d/{gameReport2DTemplateId}")]
    public async Task<IActionResult> Report2D(int projectId, int gameReport2DTemplateId)
    {
        var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId);

        if (field == null)
        {
            return NotFound();
        }

        var template =
            field.Project.GameReport2DTemplates.SingleOrDefault(
                t => t.GameReport2DTemplateId == gameReport2DTemplateId);

        if (template == null)
        {
            return NotFound();
        }

        var report2DResultViewModel = new Report2DResultViewModel(template);

        return View(report2DResultViewModel);
    }

    public ReportsController(
        IProjectRepository projectRepository,
        IProjectService projectService,
        IUserRepository userRepository) : base(projectRepository,
            projectService,
            userRepository)
    {
    }
}
