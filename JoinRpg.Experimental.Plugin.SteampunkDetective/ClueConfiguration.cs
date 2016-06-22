using System.Collections.Generic;
using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  [PublicAPI]
  public class ClueConfiguration
  {
    public int CluePerCharacter { get; set; }
    public int Digits { get; set; }
    public int MaxMeaningfulSignsCount { get; set; }
    public int MinNumberOfSignInClue { get; set; }
    public bool ShowHeaderClue { get; set; }
    public string QrEncryptionKey { get; set; }

    public List<int> GroupsToShowInQr { get; set; }
    public bool ConsistentGeneration { get; set; }

  public List<SignDefinition> SignDefinitions { get; set; }
  }
}