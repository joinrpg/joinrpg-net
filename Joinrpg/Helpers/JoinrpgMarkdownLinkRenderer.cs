using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Joinrpg.Markdown;
using JoinRpg.DataModel;
using JoinRpg.Domain;

namespace JoinRpg.Web.Helpers
{
  public class JoinrpgMarkdownLinkRenderer : ILinkRenderer
  {
    private Project Project { get; }

    public IEnumerable<string> LinkTypesToMatch => _matches.Keys.OrderByDescending(c =>c.Length);

    private readonly IReadOnlyDictionary<string, Func<string, int, string, string>> _matches;

    public JoinrpgMarkdownLinkRenderer(Project project)
    {
      Project = project;
      _matches = new Dictionary
        <string, Func<string, int, string, string>>
        {
  
  //        {"charname", Charname},
  //        {"charfull", CharacterFullFunc},
  //        {"character", CharacterFunc},
  //        {"groupname", GroupName},
  //        {"grouplist", GroupListFunc},
        };
    }

    private string GroupListFunc(string match, int index, string extra)
    {
      var group = Project.CharacterGroups.SingleOrDefault(c => c.CharacterGroupId == index);
      if (group == null)
      {
        return Fail(match, index, extra);
      }
      var groupLink = GroupLinkImpl(index, extra, group);
      var characters = Project.Characters.Where(c => c.IsPartOfGroup(group.CharacterGroupId)).Take(11).ToList();
      var builder = new StringBuilder(groupLink);
      foreach (var character in characters.Take(10))
      {
        builder.Append("<br>");
        builder.Append(CharacterImpl(character));
      }
      if (characters.Count > 10)
      {
        builder.Append("<br>....");
      }
      return $"<p>{builder}</p>";
    }

    private string GroupName(string match, int index, string extra)
    {
      var group = Project.CharacterGroups.SingleOrDefault(c => c.CharacterGroupId == index);
      if (group == null)
      {
        return Fail(match, index, extra);
      }
      return GroupLinkImpl(index, extra, group);
    }

    private string GroupLinkImpl(int index, string extra, CharacterGroup group)
    {
      var name = extra == "" ? group.CharacterGroupName : extra;
      return $"<a href=\"/{Project.ProjectId}/groups/{index}\">{name}</a>";
    }

    private string CharacterFullFunc(string match, int index, string extra)
    {
      var character = Project.Characters.SingleOrDefault(c => c.CharacterId == index);
      if (character == null)
      {
        return Fail(match, index, extra);
      }
      var characterLink = CharacterLinkImpl(character, extra);
      var vkLink = GetVKLinkImpl(character);
      var emailLink = GetEmailLinkImpl(character);
      return $"<p>{characterLink}<br>игрок: {character.ApprovedClaim?.Player?.DisplayName ?? "нет"}{emailLink}{vkLink}<br>{character.Description.ToHtmlString()}</p>";
    }

    private string CharacterFunc(string match, int index, string extra)
    {
      var character = Project.Characters.SingleOrDefault(c => c.CharacterId == index);
      if (character == null)
      {
        return Fail(match, index, extra);
      }
      return CharacterImpl(character, extra);
    }

    private string CharacterImpl(Character character, string extra = "")
    {
      var characterLink = CharacterLinkImpl(character, extra);
      var vkLink = GetVKLinkImpl(character);
      var emailLink = GetEmailLinkImpl(character);
      return
        $"<span>{characterLink} (игрок: {character.ApprovedClaim?.Player?.DisplayName ?? "нет"}{emailLink}{vkLink})</span>";
    }

    private static string GetEmailLinkImpl(Character character)
    {
      var email = character.ApprovedClaim?.Player?.Email;
      var emailLink = string.IsNullOrEmpty(email) ? "" : $" Email: <a href=\"mailto:{email}\">{email}</a>";
      return emailLink;
    }

    private static string GetVKLinkImpl(Character character)
    {
      var vk = character.ApprovedClaim?.Player?.Extra?.Vk;
      var vkLink = string.IsNullOrEmpty(vk) ? "" : $" ВК: <a href=\"https://vk.com/{vk}\">https://vk.com/{vk}</a>";
      return vkLink;
    }

    private string Charname(string match, int index, string extra)
    {
      var character = Project.Characters.SingleOrDefault(c => c.CharacterId == index);
      if (character == null)
      {
        return Fail(match, index, extra);
      }
      return CharacterLinkImpl(character, extra);
    }

    private string CharacterLinkImpl(Character character, string extra = "")
    {
      var name = extra == "" ? character.CharacterName : extra;
      return $"<a href=\"/{Project.ProjectId}/character/{character.CharacterId}\">{name}</a>";
    }

    public string Render(string match, int index, string extra)
    {
      if (match.Length > 1 && match[0] == '@' && _matches.ContainsKey(match.Substring(1)))
      {
        return _matches[match.Substring(1)](match, index, extra);
      }
      return Fail(match, index, extra);
    }

    private static string Fail(string match, int index, string extra)
    {
      if (!string.IsNullOrEmpty(extra))
      {
        extra = $"({extra}";
      }
      return $"@{match}{index}{extra}";
    }
  }
}