using System.Collections.Generic;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.HelloWorld
{

  public class HelloWorldPlugin : PluginImplementationBase
  {
    private class HellowWorldOperation : IPrintCardPluginOperation
    {
      private string Config { get; }

      public HellowWorldOperation(PluginConfiguration config)
      {
        Config = config.ConfigurationString;
      }

      public IEnumerable<HtmlCardPrintResult> PrintForCharacter(CharacterInfo character)
      {
        yield return new HtmlCardPrintResult($"Hello, {character.CharacterName}! Configuration: {Config}", CardSize.A7);
      }
    }

    public HelloWorldPlugin()
    {
      Register("HelloWorld", config => new HellowWorldOperation(config), @"
This is a simple plugin used to demonstrate ability to create **print card plugins**.
");
    }

    public override string GetName() => "JoinRpg.HelloWorld";
  }

}