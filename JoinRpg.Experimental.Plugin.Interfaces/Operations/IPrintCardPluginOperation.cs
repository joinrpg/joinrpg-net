using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  [PublicAPI]
  public interface IPrintCardPluginOperation : IPluginOperation
  {
    [NotNull, ItemNotNull]
    IEnumerable<HtmlCardPrintResult> PrintForCharacter([NotNull] CharacterInfo character);
  }
}