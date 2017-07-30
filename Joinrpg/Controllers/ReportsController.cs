using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models.Reports;

namespace JoinRpg.Web.Controllers
{
  [MasterAuthorize()]
  public class ReportsController : ControllerGameBase
  {
    public async Task<ActionResult> Report2D(int projectId, int gameReport2DTemplateId)
    {
      var field = await ProjectRepository.LoadGroupWithTreeAsync(projectId);

      if (field == null) return HttpNotFound();

      var template =
        field.Project.GameReport2DTemplates.SingleOrDefault(
          t => t.GameReport2DTemplateId == gameReport2DTemplateId);

      if (template == null) return HttpNotFound();

      var report2DResultViewModel = new Report2DResultViewModel(template);

      return View(report2DResultViewModel);
    }

    public ReportsController(ApplicationUserManager userManager,
      [NotNull] IProjectRepository projectRepository, IProjectService projectService,
      IExportDataService exportDataService) : base(userManager, projectRepository, projectService,
      exportDataService)
    {
    }
  }
}