using System.Threading.Tasks;
using System.Web.Mvc;
using Joinrpg.Markdown;
using JoinRpg.Data.Interfaces;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;

namespace JoinRpg.Web.Controllers
{
  public class PluginSetupController : ControllerGameBase
  {
    private IPluginFactory PluginFactory { get; }

    public PluginSetupController(ApplicationUserManager userManager, IPluginFactory pluginFactory,
      IProjectRepository projectRepository, IProjectService projectService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      PluginFactory = pluginFactory;
    }

    [MasterAuthorize()]
    public async Task<ActionResult> DisplayConfig(int projectid, string plugin)
    {
      var pluginInstance = await PluginFactory.GetOperationInstance<IShowConfigurationPluginOperation>(projectid, plugin);
      if (pluginInstance == null)
      {
        return HttpNotFound();
      }

      var project = await ProjectRepository.GetProjectAsync(projectid);

      ViewBag.Title = pluginInstance.OperationName;
      return View("ShowMarkdown",
        PluginFactory.ShowPluginConfiguration(pluginInstance, project).ToHtmlString());
      ;
    }
  }
}