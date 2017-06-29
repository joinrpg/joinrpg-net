using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
    public class DetectivePlugin : PluginImplementationBase
    {
      public DetectivePlugin()
      {
        Register("Улики", c => new CluePrinterOperation(c), "Распечатка улик к игре стимпанк");
        Register("Настройки", c => new ShowDetectiveConfiguration(c), "Конфигурация улик (в правильном виде)");
      }

      public override string GetName() => "Стимпанк Детектив";
    }
}
