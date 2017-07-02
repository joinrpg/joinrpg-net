using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using JoinRpg.PluginHost.Interfaces;

namespace JoinRpg.Web.Models.Plugins
{
  public class PluginListViewModel : IProjectIdAware
  {
    public PluginListViewModel(int projectId, IEnumerable<ProjectPluginInfo> pluginInfo, int currentUserId)
    {
      ProjectId = projectId;
      Plugins = pluginInfo.Select(pi => new PluginListItemViewModel(pi)).ToList();
    }

    public int ProjectId { get; }
    public IReadOnlyCollection<PluginListItemViewModel> Plugins { get; }
  }

  public class PluginListItemViewModel
  {
    public PluginListItemViewModel(ProjectPluginInfo pi)
    {
      Name = pi.Name;
      Installed = pi.Installed;
      StaticPages = pi.StaticPages;
      Description = pi.Description;
    }

    [Display(Name = "Страницы")]
    public IReadOnlyCollection<string> StaticPages { get; }

    [Display(Name = "Имя плагина")]
    public string Name { get; }

    [Display(Name = "Описание")]
    public string Description { get; }
    public bool Installed { get; }
  }
}