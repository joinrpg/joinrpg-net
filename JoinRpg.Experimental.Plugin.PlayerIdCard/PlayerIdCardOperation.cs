using System.Collections.Generic;
using System.Linq;
using JoinRpg.Experimental.Plugin.Interfaces;

namespace JoinRpg.Experimental.Plugin.PlayerIdCard
{
  public class PlayerIdCardOperation : IPrintCardPluginOperation
  {
    private PlayerCardConfiguration ParsedConfig { get; }
    public string ProjectName { get; }

    internal PlayerIdCardOperation(PlayerCardConfiguration config, string projectName)
    {
      ParsedConfig = config;
      ProjectName = projectName;
    }

    public IEnumerable<HtmlCardPrintResult> PrintForCharacter(CharacterInfo character)
    {
      if (character.HasPlayer)
      {
        yield return PrintOneCardForCharacter(character);
      }
    }

    private HtmlCardPrintResult PrintOneCardForCharacter(CharacterInfo character)
    {
      return new HtmlCardPrintResult($@"
<b>{ProjectName}</b><br>
<b>Игрок{character.PlayerId}</b>: {character.PlayerName}<br>
<b>ФИО</b>: {character.PlayerFullName}<br>
<b>Персонаж</b>: {character.CharacterName}
{string.Join("",  ParsedConfig.Fields.Select(
        parsedConfigField =>
          GetCardField(character, parsedConfigField)))}
<hr>
Особые отметки:
", CardSize.A7, ParsedConfig.LogoUrl);
    }

    private static string GetCardField(CharacterInfo character, MagicFieldConfig parsedConfigField)
    {
      var labels = string.Join(", ",
        parsedConfigField.Groups.Join(character.Groups, o => o.GroupId, i => i.CharacterGroupId,
          (o, i) => o.AddToLabel));

      return labels == "" ? "" : $"<br><b>{parsedConfigField.Label}</b>: {labels}";
    }
  }
}
