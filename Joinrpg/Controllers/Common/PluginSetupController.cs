using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Controllers.Common
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

    public async Task<ActionResult> DisplayConfig(int projectid, string plugin)
    {
      var pluginInstance = await PluginFactory.GetOperationInstance<IShowConfigurationPluginOperation>(projectid, plugin);
      if (pluginInstance == null)
      {
        return HttpNotFound();
      }

      var groups = await ProjectRepository.GetCharacters(projectid);

      var error = await AsMaster(groups.ToArray(), projectid);
      if (error != null) return error;

      ViewBag.Title = pluginInstance.OperationName;
      return View("ShowMarkdown",
        new MarkdownViewModel(PluginFactory.ShowPluginConfiguration(pluginInstance,
          await GetProjectFromList(projectid, groups))));
      ;
    }
  }
}