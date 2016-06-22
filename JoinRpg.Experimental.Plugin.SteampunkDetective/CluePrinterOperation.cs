using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
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
      public bool Equals(SignDefinition x, SignDefinition y) => x.FieldId == y.FieldId && x.FieldId !=0;

      public int GetHashCode(SignDefinition obj) => obj.FieldId;
    }

    private ClueConfiguration Config { get; }


    public CluePrinterOperation([NotNull] string config)
    {
      if (config == null) throw new ArgumentNullException(nameof(config));

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
          throw new PluginConfigurationIncorrectException($"Code {signDefinition.Code} is too large. Should be less than {MaxCode}");
        }
        if (UsedCodes.Contains(signDefinition.Code))
        {
          throw new PluginConfigurationIncorrectException($"Code {signDefinition.Code} is duplicated");
        }
        UsedCodes.Add(signDefinition.Code);
      }
    }

    private int MaxCode { get; }
    private HashSet<int> UsedCodes { get; } = new HashSet<int>();

    public IEnumerable<HtmlCardPrintResult> PrintForCharacter(CharacterInfo character)
    {
      var possibleSigns =
        Config.SignDefinitions.Where(sign => sign.IsValidForCharacter(character))
          .SelectMany(s => Enumerable.Repeat(s, s.Weight))
          .ToArray();
      var fieldsToShowInQrCode = character.Fields.Where(f => possibleSigns.Any(s => s.FieldId == f.FieldId));
      var characterMasterDesc =
        $"{character.CharacterName} ({character.Groups.Select(g => g.ToString()).JoinStrings(",")}) {fieldsToShowInQrCode.Select(f => f.ToString()).JoinStrings(", ")}";

      if (Config.ShowHeaderClue)
      {
        yield return new HtmlCardPrintResult($"<div style='text-align:center'>{characterMasterDesc}</h2>", CardSize.A7);
      }

      var random = Config.ConsistentGeneration ? new Random(character.CharacterId) : new Random();

      
      for (var i = 0; i < Config.CluePerCharacter; i++)
      {
        var characterQrCodeContents =
        $"{i}.{character.CharacterId} {character.Groups.Where(g => Config.GroupsToShowInQr.Contains(g.CharacterGroupId)).Select(g => g.ToString()).JoinStrings(",")} {character.CharacterName}";
        var qrCodeForCharacter = GenerateQrCodeForCharacter(character, characterQrCodeContents);
        yield return GenerateClue(character, possibleSigns, random, qrCodeForCharacter);
      }
    }

    private HtmlCardPrintResult GenerateClue(CharacterInfo character, IReadOnlyCollection<SignDefinition> possibleSigns, Random random, string embeddedImageTag)
    {
      var randomCodes = random.GetRandomSource().Select(c => c % MaxCode).Where(c => !UsedCodes.Contains(c));

      var signs = possibleSigns
        .Shuffle(random)
        .Distinct(ClueCompareByFieldId)
        .Take(Config.MaxMeaningfulSignsCount)
        .Select(c => c.Code)
        .ToArray()
        .UnionUntilTotalCount(randomCodes, Config.MinNumberOfSignInClue)
        .Shuffle(random);
      var signString = signs
        .Select(c => c.ToString("D3"))
        .JoinStrings(" ");

      return
        new HtmlCardPrintResult(
          $@"
<div style='text-align:center'>Карточка улики. <br>
Оставьте ее на месте, если вы не детектив <br> 
{signString} 
</div>
<div style='text-align:center; vertical-align: bottom'>
  {embeddedImageTag}
</div>
",
          CardSize.A7);
    }

    private string GenerateQrCodeForCharacter(CharacterInfo character, string qrCodeContents)
    {
      var qrGenerator = new QRCodeGenerator();
      var utf8Bytes = StaticStringHelpers.AsUtf8BytesWithLimit(qrCodeContents, 90).ToArray();

      var key = Config.QrEncryptionKey;
      var encryptedBytes = key != null ? BouncyFacade.Encrypt(key, utf8Bytes) : Convert.ToBase64String(utf8Bytes);

      var qrCodeData = qrGenerator.CreateQrCode(encryptedBytes, QRCodeGenerator.ECCLevel.H);
      var qrCode = new QRCode(qrCodeData);
      var qrCodeImage = qrCode.GetGraphic(pixelsPerModule: 2);
      return qrCodeImage.ToEmbeddedImageTag();
    }
  }
}