using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JoinRpg.DataModel;
using JoinRpg.Experimental.Plugin.Interfaces;
using JoinRpg.Helpers;

namespace JoinRpg.Experimental.Plugin.SteampunkDetective
{
  public class ShowDetectiveConfiguration : IStaticPagePluginOperation
  {
    private ClueConfiguration Config { get; set; }

    public MarkdownString ShowStaticPage(IEnumerable<CharacterGroupInfo> projectGroups, IEnumerable<ProjectFieldInfo> fields)
    {
      var buffer = new StringBuilder();
      var fieldBuf = fields.ToArray();
      var groupBuf = projectGroups.ToArray();
      foreach (var signDefinition in Config.SignDefinitions)
      {
        buffer.AppendLine($"{signDefinition.Code}\n--");
        buffer.AppendLine($"Вес {signDefinition.Weight}\n");
        if (signDefinition.FieldId != 0)
        {
          var field = fieldBuf.SingleOrDefault(f => f.FieldId == signDefinition.FieldId);
          buffer.Append(
            $"**{field?.FieldName ?? "??"}** = (");
          if (field != null)
          {
            var grs =
              field.Values.Where(kv => signDefinition.AllowedValues.Contains(kv.Key.ToString())).Select(
                fv => fv.Value);
            buffer.Append(grs.JoinStrings(", "));
            buffer.AppendLine(")\n");
          }

        }
        AddGroupList(buffer, groupBuf, "Показывать для групп", signDefinition.ShowForGroups);
        AddGroupList(buffer, groupBuf, "Скрывать для групп", signDefinition.SkipForGroups);

        buffer.AppendLine();
      }
      return new MarkdownString(buffer.ToString());
    }

    private static void AddGroupList(StringBuilder buffer, CharacterGroupInfo[] groupBuf, string label, int[] list)
    {
      if (list.Any())
      {
        buffer.Append($"**{label}**:");
      }
      var grs =
        list.Select(
          groupId => groupBuf.SingleOrDefault(f => f.CharacterGroupId == groupId)?.CharacterGroupName ?? "??");
      buffer.AppendLine(grs.JoinStrings(", "));
      buffer.AppendLine();
    }

    public void SetConfiguration(IPluginConfiguration config)
    {
      if (config == null) throw new ArgumentNullException(nameof(config));

      Config = config.GetConfiguration<ClueConfiguration>();
    }
  }
}