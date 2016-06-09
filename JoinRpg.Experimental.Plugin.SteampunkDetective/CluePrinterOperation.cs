using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using Newtonsoft.Json;
using QRCoder;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public class CluePrinterOperation : IPrintCardPluginOperation
  {
    private static readonly CompareCluesByFieldId ClueCompareByFieldId = new CompareCluesByFieldId();

    private class CompareCluesByFieldId : IEqualityComparer<SignDefinition>
    {
      public bool Equals(SignDefinition x, SignDefinition y) => x.SignType == y.SignType && x.FieldId == y.FieldId;

      public int GetHashCode(SignDefinition obj) => (int)obj.SignType ^ obj.FieldId;
    }

    private ClueConfiguration Config { get; }


    public CluePrinterOperation(string config)
    { 
      // Config = JsonConvert.DeserializeObject<ClueConfiguration>(config);
      Config = CluePrinterDefaultConfig.GetDefaultConfig();

      

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
      var random = new Random(character.CharacterId); //Same seed everytime for consistent generation

      for (var i = 0; i < Config.CluePerCharacter; i++)
      {
        
        yield return GenerateClue(character, possibleSigns, random);
      }
    }

    private bool IsValidDefinition(CharacterInfo character, SignDefinition sign)
    {
      switch (sign.SignType)
      {
        case SignType.FieldBased:
          var field = character.Fields.SingleOrDefault(f => f.FieldId == sign.FieldId);
          return sign.AllowedFieldValues.Contains(field?.FieldValue);
        case SignType.GroupBased:
          return sign.AllowedFieldValues.Intersect(character.Groups.Select(g => g.CharacterGroupId.ToString())).Any();
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private HtmlCardPrintResult GenerateClue(CharacterInfo character, IReadOnlyCollection<SignDefinition> possibleSigns, Random random)
    {
      QRCodeGenerator qrGenerator = new QRCodeGenerator();
      QRCodeData qrCodeData = qrGenerator.CreateQrCode(character.CharacterName, QRCodeGenerator.ECCLevel.Q);
      var qrCode = new QRCode(qrCodeData);
      var qrCodeImage = qrCode.GetGraphic(pixelsPerModule: 1);

      var randomCodes = random.GetRandomSource().Select(c => c%MaxCode).Where(c => !UsedCodes.Contains(c));

      var signs = possibleSigns
        .Shuffle(random)
        .Distinct(ClueCompareByFieldId)
        .Take(Config.MaxMeaningfulClues)
        .Select(c => c.Code)
        .ToArray()
        .UnionUntilTotalCount(randomCodes, Config.RequiredClues)
        .Shuffle(random);
      var signString = signs
        .Select(c => c.ToString("D3"))
        .JoinStrings(" ");
      return
        new HtmlCardPrintResult($"{character.CharacterName} (имя указано для теста) <br> {signString} {qrCodeImage.ToEmbeddedImageTag()}", CardSize.A7);
    }
  }
}