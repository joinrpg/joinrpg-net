using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
    public class DetectivePlugin : PluginImplementationBase
    {
      public DetectivePlugin()
      {
        Register("Улики", c => new CluePrinterOperation(c), "Распечатка улик к игре стимпанк");
        Register("Конфиг", c => new DetectiveConfiguration(c), "Конфигурация улик");
      }
      public override string GetName() => "Стимпанк Детектив";
    }
}
