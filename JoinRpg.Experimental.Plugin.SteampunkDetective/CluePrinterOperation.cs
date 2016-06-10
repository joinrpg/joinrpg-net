using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
      Config = JsonConvert.DeserializeObject<ClueConfiguration>(config);

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
      var possibleSigns = Config.SignDefinitions.Where(sign => sign.IsValidForCharacter(character)).ToArray();
      var random = new Random(character.CharacterId); //Same seed everytime for consistent generation

      var fieldsToShowInQrCode = character.Fields.Where(f => possibleSigns.Any(s => s.FieldId == f.FieldId));
      var qrCodeForCharacter = GenerateQrCodeForCharacter(character,
        fieldsToShowInQrCode);
      for (var i = 0; i < Config.CluePerCharacter; i++)
      {
        yield return GenerateClue(character, possibleSigns, random, qrCodeForCharacter);
      }
    }

    private HtmlCardPrintResult GenerateClue(CharacterInfo character, IReadOnlyCollection<SignDefinition> possibleSigns, Random random, string embeddedImageTag)
    {
      var randomCodes = random.GetRandomSource().Select(c => c % MaxCode).Where(c => !UsedCodes.Contains(c));

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
        new HtmlCardPrintResult(
          $@"
<div style='text-align:center'>Карточка улики. 
Оставьте ее на месте, если вы не детектив <br> 
{signString} 
</div>
<div style='text-align:center; vertical-align: bottom'>
  {embeddedImageTag}
</div>
",
          CardSize.A7);
    }

    private static string GenerateQrCodeForCharacter(CharacterInfo character, IEnumerable<CharacterFieldInfo> applicableGroups)
    {
      QRCodeGenerator qrGenerator = new QRCodeGenerator();
      var formattableString =
        $"{character.CharacterName} ({character.Groups.Select(g => g.ToString()).JoinStrings(",")}) {applicableGroups.Select(f => f.ToString()).JoinStrings(",")}";
      var limitedString = LimitUtf8Bytes(formattableString, 600).AsString();
      QRCodeData qrCodeData =
        qrGenerator.CreateQrCode(
          limitedString,
          QRCodeGenerator.ECCLevel.L);
      var qrCode = new QRCode(qrCodeData);
      var qrCodeImage = qrCode.GetGraphic(pixelsPerModule: 2);
      var embeddedImageTag = qrCodeImage.ToEmbeddedImageTag();
      return embeddedImageTag;
    }

    private static IEnumerable<char> LimitUtf8Bytes(string formattableString, int i)
    {
      var limit = i;
      foreach (var character in formattableString)
      {
        var encoded = Encoding.UTF8.GetByteCount( new [] {character});
        if (limit < encoded + 3)
        {
          yield return '.';
          yield return '.';
          yield return '.';
          yield break;
        }
        limit -= encoded;
        yield return character;
      }
    }
  }
}