using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.PlayerIdCard
{
  public class PlayerIdCardPlugin : PluginImplementationBase
  {
    public PlayerIdCardPlugin()
    {
      Register<PlayerIdCardOperation>("Распечатка аусвайсов");
    }

    public override string GetName() => "Распечатка аусвайсов";
    public override string GetDescripton() => "Предназначен для игры Третья Эпоха и позволяет печатать аусвайсы.";
  }
}
