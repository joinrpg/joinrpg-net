using System.Collections.Generic;
using System.Linq;
using JoinRpg.Experimental.Plugin.Interfaces;
using Newtonsoft.Json;

namespace JoinRpg.Experimental.Plugin.PlayerIdCard
{
  public class PlayerIdCardOperation : IPrintCardPluginOperation
  {
    private readonly PluginConfiguration _config;

    private PlayerCardConfiguration ParsedConfig { get; }

    public PlayerIdCardOperation(PluginConfiguration config)
    {
      _config = config;
      ParsedConfig = JsonConvert.DeserializeObject<PlayerCardConfiguration>(config.ConfigurationString);
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
<b>{_config.ProjectName}</b><br>
<b>Игрок</b>: {character.PlayerName}<br>
<b>ФИО</b>: {character.PlayerFullName}<br>
<b>Персонаж</b>: {character.CharacterName}
{string.Join("",  ParsedConfig.Fields.Select(
        parsedConfigField =>
          GetCardField(character, parsedConfigField)))}
<hr>
Особые отметки:
", CardSize.A7);
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