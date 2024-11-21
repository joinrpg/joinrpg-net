using System.Text;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Web.Helpers;

public class JoinrpgMarkdownLinkRenderer : ILinkRenderer
{
    private Project Project { get; }

    public string[] LinkTypesToMatch { get; }

    private readonly Dictionary<string, Func<string, int, string, string>> matches;

    public JoinrpgMarkdownLinkRenderer(Project project)
    {
        Project = project;
        matches = new Dictionary
          <string, Func<string, int, string, string>>
        {
          {"персонаж", Charname},
          {"контакты", CharacterFullFunc},
          {"группа", GroupName},
          {"список", GroupListFunc},
          {"сеткаролей", GroupListFullFunc},
        };

        LinkTypesToMatch = [.. matches.Keys.OrderByDescending(c => c.Length)];
    }

    private string GroupListFunc(string match, int index, string extra)
    {
        var group = Project.CharacterGroups.SingleOrDefault(c => c.CharacterGroupId == index);
        if (group == null)
        {
            return Fail(match, index, extra);
        }
        var groupLink = GroupLinkImpl(index, extra, group);
        var characters = GetGroupCharacters(group).Select(c => CharacterImpl(c));
        var builder = new StringBuilder();
        foreach (var character in characters)
        {
            _ = builder.Append("<br>").Append(character);
        }
        return $"<h4>Группа: {groupLink}</h4><p>{builder}</p>";
    }

    private string GroupListFullFunc(string match, int index, string extra)
    {
        var group = Project.CharacterGroups.SingleOrDefault(c => c.CharacterGroupId == index);
        if (group == null)
        {
            return Fail(match, index, extra);
        }
        var groupLink = GroupLinkImpl(index, extra, group);
        var characters = GetGroupCharacters(group);
        var builder = new StringBuilder();
        foreach (var character in characters)
        {
            _ = builder.Append($"<p>&nbsp;<b>{CharacterImpl(character)}</b><br>{character.Description.ToHtmlString()}</p>");
        }
        return $"<h4>Группа: {groupLink}</h4>{group.Description.ToHtmlString()}{builder}<hr>";
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
            return (character.CharacterType, character.ApprovedClaim?.Player) switch
            {
                (CharacterType.NonPlayer, _) => "NPC",
                (CharacterType.Slot, _) => "шаблон",
                (CharacterType.Player, null) => "нет игрока",
                (CharacterType.Player, User player) => GetPlayerContacts(player),
                _ => throw new NotImplementedException(),
            };
        }

        string GetPlayerContacts(User player)
        {
            var contacts = new[] { GetEmailLinkImpl(player), GetVKLinkImpl(player), GetTelegramLinkImpl(player) };
            return $"{player.GetDisplayName()}: {contacts.JoinIfNotNullOrWhitespace(", ")}";
        }
    }

    private static string? GetEmailLinkImpl(User player)
    {
        var email = player.Email;
        return string.IsNullOrEmpty(email) ? null : $"Email: <a href=\"mailto:{email}\">{email}</a>";
    }

    private static string? GetVKLinkImpl(User player)
    {
        var vk = player.Extra?.Vk;
        return string.IsNullOrEmpty(vk) ? null : $"ВК: <a href=\"https://vk.com/{vk}\">vk.com/{vk}</a>";
    }

    private static string? GetTelegramLinkImpl(User player)
    {
        var link = player.Extra?.Telegram?.TrimStart('@');
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
        if (match.Length > 1 && match[0] == '%' && matches.ContainsKey(match[1..]))
        {
            return matches[match[1..]](match, index, extra);
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

    private static IEnumerable<Character> GetGroupCharacters(CharacterGroup group)
    {
        return group.GetOrderedCharacters()
                    .Union(
                        group.GetOrderedChildrenGroupsRecursive().SelectMany(g => g.GetOrderedCharacters().Where(chr => chr.IsActive))
                        )
                    .Distinct();
    }
}
