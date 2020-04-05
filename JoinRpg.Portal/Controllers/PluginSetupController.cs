using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Experimental.Plugin.Interfaces;
using Joinrpg.Markdown;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Plugins;

namespace JoinRpg.Portal.Controllers
{
    [Route("{projectId}/plugins")]
  public class PluginSetupController : ControllerGameBase
  {
    private IPluginFactory PluginFactory { get; }

    public PluginSetupController(
        IPluginFactory pluginFactory,
        IProjectRepository projectRepository,
        IProjectService projectService,
        IUserRepository userRepository)
      : base(projectRepository, projectService, userRepository)
        {
      PluginFactory = pluginFactory;
    }

    [MasterAuthorize]
    public async Task<ActionResult> DisplayConfig(int projectid, string plugin)
    {
      var pluginInstance = await PluginFactory.GetConfiguration(projectid, plugin);
      if (pluginInstance == null)
      {
        return NotFound();
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
        return NotFound();
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
