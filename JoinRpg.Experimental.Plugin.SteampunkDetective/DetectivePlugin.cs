using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
    public class DetectivePlugin : PluginImplementationBase
    {
      public DetectivePlugin()
      {
        Register("Clue", c => new CluePrinterOperation(c), "Распечатка улик к игре стимпанк");
      }
      public override string GetName() => "SteamDetective";
    }
}
