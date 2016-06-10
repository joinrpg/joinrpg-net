using System.Collections.Generic;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public class ClueConfiguration
  {
    public int CluePerCharacter { get; set; }
    public int Digits { get; set; }
    public int MaxMeaningfulSignsCount { get; set; }
    public int MinNumberOfSignInClue { get; set; }
    public bool ShowHeaderClue { get; set; }
    public List<SignDefinition> SignDefinitions { get; set; }
  }
}