using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.PluginHost.Interfaces
{
  public interface IPluginResolver
  {
    IEnumerable<IPlugin> Resolve();
  }
}
