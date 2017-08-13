using System.Threading.Tasks;
using System.Web.Mvc;
using Joinrpg.Markdown;
using JoinRpg.Data.Interfaces;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models.Plugins;

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

    [MasterAuthorize]
    public async Task<ActionResult> DisplayConfig(int projectid, string plugin)
    {
      var pluginInstance = await PluginFactory.GetConfiguration(projectid, plugin);
      if (pluginInstance == null)
      {
        return HttpNotFound();
      }

      ViewBag.Title = pluginInstance.Name;
      return View("ShowMarkdown", pluginInstance.Configuration.ToHtmlString());
    }

    [MasterAuthorize]
    public async Task<ActionResult> DisplayPage(int projectid, string operation)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      var pluginInstance = PluginFactory.GetOperationInstance<IStaticPagePluginOperation>(project, operation);
      if (pluginInstance == null)
      {
        return HttpNotFound();
      }

      ViewBag.Title = pluginInstance.OperationName;
      return View("ShowMarkdown",
        PluginFactory.ShowStaticPage(pluginInstance, project).ToHtmlString());
    }

    [MasterAuthorize()]
    public async Task<ActionResult> Index(int projectid)
    {
      var plugins = await PluginFactory.GetPluginsForProject(projectid);

      return View(new PluginListViewModel(projectid, plugins, CurrentUserId));
    }
  }
}