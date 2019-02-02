using System.Collections.Generic;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.PluginHost.Interfaces;

namespace JoinRpg.Web.Models.Print
{
  public class PrintIndexViewModel
  {
    public PrintIndexViewModel(int projectId, IReadOnlyCollection<int> characterIds, IEnumerable<PluginOperationDescriptionViewModel> plugins)
    {
      Plugins = plugins;
      ProjectId = projectId;
      CharacterIds = characterIds;
    }

    public IEnumerable<PluginOperationDescriptionViewModel> Plugins { get; }
    
    public int ProjectId { get; }
    public IReadOnlyCollection<int> CharacterIds { get;  }
  }

  public class PluginOperationDescriptionViewModel
  {
    public static PluginOperationDescriptionViewModel Create<T> (PluginOperationData<T> p) where T : IPluginOperation
    {
      return new PluginOperationDescriptionViewModel()
      {
        Name = p.OperationName,
        Description = p.Description,
      };
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
  }
}