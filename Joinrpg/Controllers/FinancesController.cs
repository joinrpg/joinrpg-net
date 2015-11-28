using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class FinancesController : Common.ControllerGameBase
  {
    
    public async Task<ActionResult> Setup(int projectid)
    {
      return AsMaster(await ProjectRepository.GetProjectAsync(projectid)) ?? View();
    }

    public FinancesController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
    }

    public async Task<ActionResult> List(int projectid, string export)
    {

      var project = await ProjectRepository.GetProjectWithFinances(projectid);
      var errorResult = AsMaster(project);
      if (errorResult != null)
      {
        return errorResult;
      }
      var viewModel = project.FinanceOperations.Where(fo => !fo.RequireModeration)
        .OrderBy(f => f.OperationDate)
        .Select(FinOperationListItemViewModel.Create);

      if (export == null)
      {
        return View(viewModel);
      }
      else
      {
        return await Export(viewModel, "finance-export", GetExportTypeByName(export));
      }
    }

    private ExportType GetExportTypeByName(string export)
    {
      switch (export)
      {
        case "csv": return ExportType.Csv;
        case "xlsx": return ExportType.ExcelXml;
        default:
          throw new ArgumentOutOfRangeException(nameof(export));
      }
    }

    private async Task<FileContentResult> Export<T>(IEnumerable<T> @select, string fileName, ExportType exportType = ExportType.Csv)
    {
      var generator = ExportDataService.GetGenerator(exportType, @select);
      return File(await generator.Generate(), generator.ContentType, Path.ChangeExtension(fileName, generator.FileExtension));
    }
  }
}