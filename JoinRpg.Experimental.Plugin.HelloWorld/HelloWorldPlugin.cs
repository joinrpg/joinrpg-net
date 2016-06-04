using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.HelloWorld
{

  public class HelloWorldPlugin : PluginImplementationBase
  {
    private class HellowWorldOperation : IPrintCardPluginOperation
    {
      private string Config { get; }

      public HellowWorldOperation(string config)
      {
        Config = config;
      }

      public IEnumerable<HtmlCardPrintResult> PrintForCharacters(IEnumerable<Character> characters)
      {
        return
          characters.Select(
            character =>
              new HtmlCardPrintResult($"Hello, {character.CharacterName}! Configuration: {Config}", CardSize.A6));
      }
    }

    public HelloWorldPlugin()
    {
      Register("HelloWorld", config => new HellowWorldOperation(config));
    }

    public override string GetName() => "JoinRpg.HelloWorld";
  }

}