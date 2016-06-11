using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public interface IPrintCardPluginOperation : IPluginOperation
  {
    IEnumerable<HtmlCardPrintResult> PrintForCharacter(CharacterInfo character);
  }
}