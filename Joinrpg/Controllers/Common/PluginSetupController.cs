using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Controllers.Common
{
  public class PluginSetupController : ControllerBase
  {
    private IPluginFactory PluginFactory { get; }

    public PluginSetupController(ApplicationUserManager userManager, IPluginFactory pluginFactory) : base(userManager)
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

      ViewBag.Title = pluginInstance.OperationName;
      return View("ShowMarkdown", new MarkdownViewModel(PluginFactory.ShowPluginConfiguration(pluginInstance)));
    }
  }
}