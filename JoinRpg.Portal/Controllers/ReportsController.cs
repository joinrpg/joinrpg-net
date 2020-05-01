using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Reports;

namespace JoinRpg.Portal.Controllers
{
    [MasterAuthorize()]
    public class ReportsController : ControllerGameBase
    {
        [HttpGet("{projectId}/reports/2d/{gameReport2DTemplateId}")]
        public async Task<IActionResult> Report2D(int projectId, int gameReport2DTemplateId)
        {
            var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId);

            if (field == null) return NotFound();

            var template =
                field.Project.GameReport2DTemplates.SingleOrDefault(
                    t => t.GameReport2DTemplateId == gameReport2DTemplateId);

            if (template == null) return NotFound();

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
}
