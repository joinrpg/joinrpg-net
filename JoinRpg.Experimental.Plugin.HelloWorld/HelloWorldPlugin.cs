using System.Collections.Generic;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.HelloWorld
{

  public class HelloWorldPlugin : PluginImplementationBase
  {
    private class HellowWorldOperation : IPrintCardPluginOperation
    {
      private string Config { get; set; }

      public IEnumerable<HtmlCardPrintResult> PrintForCharacter(CharacterInfo character)
      {
        yield return new HtmlCardPrintResult($"Hello, {character.CharacterName}! Configuration: {Config}", CardSize.A7);
      }

      public void SetConfiguration(IPluginConfiguration config)
      {
        Config = config.GetConfiguration<string>();
      }
    }

    public HelloWorldPlugin()
    {
      Register<HellowWorldOperation>("HelloWorld", @"
This is a simple plugin used to demonstrate ability to create **print card plugins**.
");
    }

    public override string GetName() => "JoinRpg.HelloWorld";
    public override string GetDescripton() => "Не содержит никакого полезного функционала, просто демонстрирует на что способны плагины";
  }

}