using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.Helpers;
using Newtonsoft.Json;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public class CluePrinterOperation : IPrintCardPluginOperation
  {
    private Random Random { get; } = new Random();
    private ClueConfiguration Config { get; }

    private IEnumerable<int> RandomCodes { get; }

    public CluePrinterOperation(string config)
    { 
      // Config = JsonConvert.DeserializeObject<ClueConfiguration>(config);
      Config = CluePrinterDefaultConfig.GetDefaultConfig();

      RandomCodes = Random.GetRandomSource().Select(c => c % MaxCode).Where(c => !UsedCodes.Contains(c));

      var maxCode = 1;
      var digits = Config.Digits;
      while (digits > 0)
      {
        maxCode *= 10;
        digits--;
      }
      MaxCode = maxCode - 1;

      foreach (var signDefinition in Config.SignDefinitions)
      {
        if (signDefinition.Code > MaxCode)
        {
          throw new PluginConfigurationIncorrectException();
        }
        if (UsedCodes.Contains(signDefinition.Code))
        {
          throw new PluginConfigurationIncorrectException();
        }
        UsedCodes.Add(signDefinition.Code);
      }
    }

    private int MaxCode { get; }
    private HashSet<int> UsedCodes { get; } = new HashSet<int>();

    public IEnumerable<HtmlCardPrintResult> PrintForCharacter(CharacterInfo character)
    {
      var possibleSigns = Config.SignDefinitions.Where(sign => IsValidDefinition(character, sign)).ToArray();
      for (var i = 0; i < Config.CluePerCharacter; i++)
      {
        yield return GenerateClue(character, possibleSigns);
      }
    }

    private bool IsValidDefinition(CharacterInfo character, SignDefinition sign)
    {
      switch (sign.SignType)
      {
        case SignType.FieldBased:
          var field = character.Fields.SingleOrDefault(f => f.FieldId == sign.FieldId);
          return sign.AllowedFieldValues.Contains(field?.FieldValue);
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private HtmlCardPrintResult GenerateClue(CharacterInfo character, IReadOnlyCollection<SignDefinition> possibleSigns)
    {
      var signs = possibleSigns
        .Shuffle(Random)
        .Take(Config.MaxMeaningfulClues)
        .Select(c => c.Code)
        .ToArray()
        .UnionUntilTotalCount(RandomCodes, Config.RequiredClues)
        .Shuffle(Random);
      return
        new HtmlCardPrintResult($"{character.CharacterName} (имя указано для теста) <br> " +
          signs
            .Select(c => c.ToString("D3"))
            .Join(" "), CardSize.A6);
    }
  }
}