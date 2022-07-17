using System.Text;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;

namespace JoinRpg.Web.Helpers;

public class JoinrpgMarkdownLinkRenderer : ILinkRenderer
{
    private Project Project { get; }

    public IEnumerable<string> LinkTypesToMatch => _matches.Keys.OrderByDescending(c => c.Length);

    private readonly IReadOnlyDictionary<string, Func<string, int, string, string>> _matches;

    public JoinrpgMarkdownLinkRenderer(Project project)
    {
        Project = project;
        _matches = new Dictionary
          <string, Func<string, int, string, string>>
    {
      {"персонаж", Charname},
      {"контакты", CharacterFullFunc},
      {"группа", GroupName},
      {"список", GroupListFunc},
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
        var characters =
            group.GetOrderedCharacters()
            .Union(
                group.GetOrderedChildrenGroupsRecursive().SelectMany(g => g.GetOrderedCharacters())
                )
            .Distinct()
            .Where(chr => chr.IsActive)
            .Select(c => CharacterImpl(c));
        var builder = new StringBuilder();
        foreach (var character in characters)
        {
            _ = builder.Append("<br>").Append(character);
        }
        return $"<h4>Группа: {groupLink}</h4><p>{builder}</p>";
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
        return $"<a href=\"/{Project.ProjectId}/roles/{index}/details\">{name}</a>";
    }

    private string CharacterFullFunc(string match, int index, string extra)
    {
        var character = Project.Characters.SingleOrDefault(c => c.CharacterId == index);
        return character == null ? Fail(match, index, extra) : CharacterImpl(character, extra);
    }

    private string CharacterImpl(Character character, string extra = "")
    {
        return $"<span>{CharacterLinkImpl(character, extra)}&nbsp;({GetPlayerString()})</span>";

        string GetPlayerString()
        {
            if (character.IsNpc())
            {
                return "NPC";
            }
            var player = character.ApprovedClaim?.Player;

            if (player == null)
            {
                return "нет игрока";
            }

            var contacts = new[] { GetEmailLinkImpl(player), GetVKLinkImpl(player), GetTelegramLinkImpl(player) };
            return $"{player.GetDisplayName()}: {contacts.JoinIfNotNullOrWhitespace(", ")}";
        }
    }

    private static string? GetEmailLinkImpl([NotNull] User player)
    {
        var email = player.Email;
        return string.IsNullOrEmpty(email) ? null : $"Email: <a href=\"mailto:{email}\">{email}</a>";
    }

    private static string? GetVKLinkImpl([NotNull] User player)
    {
        var vk = player.Extra?.Vk;
        return string.IsNullOrEmpty(vk) ? null : $"ВК: <a href=\"https://vk.com/{vk}\">vk.com/{vk}</a>";
    }

    private static string? GetTelegramLinkImpl([NotNull] User player)
    {
        var link = player.Extra?.Telegram;
        return string.IsNullOrEmpty(link) ? null : $"Телеграм: <a href=\"https://t.me/{link}\">t.me/{link}</a>";
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
        if (match.Length > 1 && match[0] == '%' && _matches.ContainsKey(match.Substring(1)))
        {
            return _matches[match.Substring(1)](match, index, extra);
        }
        return Fail(match, index, extra);
    }

    private static string Fail(string match, int index, string extra)
    {
        if (!string.IsNullOrEmpty(extra))
        {
            extra = $"({extra})";
        }
        return $"{match}{index}{extra}";
    }
}
