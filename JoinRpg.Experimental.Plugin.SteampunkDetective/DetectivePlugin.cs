using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
    public class DetectivePlugin : PluginImplementationBase
    {
      public DetectivePlugin()
      {
        Register<CluePrinterOperation>("Улики", "Распечатка улик к игре стимпанк");
        Register<ShowDetectiveConfiguration>("Настройки", "Конфигурация улик (в правильном виде)");
      }

      public override string GetName() => "Стимпанк Детектив";
      public override string GetDescripton() => "Содержит реализацию модели детектива для игры Стимпанк";

    }
}
