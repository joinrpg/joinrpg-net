using JoinRpg.Experimental.Plugin.Interfaces;
using Newtonsoft.Json;

namespace JoinRpg.Experimental.Plugin.PlayerIdCard
{
  public class PlayerIdCardPlugin : PluginImplementationBase
  {
    public PlayerIdCardPlugin()
    {
      Register("Распечатка аусвайсов",
        config => new PlayerIdCardOperation(
          JsonConvert.DeserializeObject<PlayerCardConfiguration>(config.ConfigurationString), config.ProjectName));
    }

    public override string GetName() => "Распечатка аусвайсов";
  }
}
