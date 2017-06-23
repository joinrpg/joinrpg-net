using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.PlayerIdCard
{
  public class PlayerIdCardPlugin : PluginImplementationBase
  {
    public PlayerIdCardPlugin()
    {
      Register("Распечатка аусвайсов", config => new PlayerIdCardOperation(config));
      RegisterShowJsonConfiguraton();
    }

    public override string GetName() => "Распечатка аусвайсов";
  }
}
