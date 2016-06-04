using System.Collections.Generic;

namespace JoinRpg.Web.Models.Print
{
  public class PrintIndexViewModel
  {
    public PrintIndexViewModel(int projectId, IEnumerable<int> characterIds, IEnumerable<string> pluginNames)
    {
      PluginNames = pluginNames;
      ProjectId = projectId;
      CharacterIds = characterIds;
    }

    public IEnumerable<string> PluginNames { get; }
    public int ProjectId { get; }
    public IEnumerable<int> CharacterIds { get;  }
  }
}