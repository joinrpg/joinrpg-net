using System.Collections.Generic;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public class ClueConfiguration
  {
    public int CluePerCharacter { get; set; }
    public int Digits { get; set; }
    public int MaxMeaningfulClues { get; set; }
    public int RequiredClues { get; set; }
    public List<SignDefinition> SignDefinitions { get; set; }
  }
}