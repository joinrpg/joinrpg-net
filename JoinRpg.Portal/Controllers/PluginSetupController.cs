using System.Threading.Tasks;
using Joinrpg.Markdown;
using JoinRpg.Data.Interfaces;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Plugins;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Route("{projectId}/plugins/[action]")]
    [MasterAuthorize()]
    public class PluginSetupController : ControllerGameBase
    {
        private IPluginFactory PluginFactory { get; }

        public PluginSetupController(
            IPluginFactory pluginFactory,
            IProjectRepository projectRepository,
            IProjectService projectService,
            IUserRepository userRepository)
          : base(projectRepository, projectService, userRepository) => PluginFactory = pluginFactory;

        [HttpGet]
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

        [HttpGet]
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

        [HttpGet]
        public async Task<ActionResult> Index(int projectid)
        {
            var plugins = await PluginFactory.GetPluginsForProject(projectid);

            return View(new PluginListViewModel(projectid, plugins, CurrentUserId));
        }
    }
}
