using System.Collections.Generic;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public interface IPrintCardPluginOperation : IPluginOperation
  {
    IEnumerable<HtmlCardPrintResult> PrintForCharacters(IEnumerable<Character> character);
  }
}