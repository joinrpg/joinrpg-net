using System.Text;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Web.Helpers;

public class JoinrpgMarkdownLinkRenderer : ILinkRenderer
{
    private Project Project { get; }

    public string[] LinkTypesToMatch { get; }

    private readonly Dictionary<string, Func<string, int, string, string>> matches;
    private readonly ProjectInfo projectInfo;

    public JoinrpgMarkdownLinkRenderer(Project project, ProjectInfo projectInfo)
    {
        Project = project;
        this.projectInfo = projectInfo;
        matches = new Dictionary
          <string, Func<string, int, string, string>>
        {
          {"персонаж", CharWrapper(CharacterLinkImpl) },
          {"контакты", CharWrapper(CharacterImpl) },
          {"группа", GroupWrapper(GroupName)},
          {"список", GroupWrapper(GroupListFunc)},
          {"сеткаролей", GroupWrapper(GroupListFullFunc)},
          {"экспериментальнаятаблица", GroupWrapper(ExperimentalTableFunc) }
        };

        LinkTypesToMatch = [.. matches.Keys.OrderByDescending(c => c.Length)];
    }

    private string ExperimentalTableFunc(int groupId, string extra, CharacterGroup group, IEnumerable<Character> characters)
    {
        var groupLink = GroupLinkImpl(group, "");

        var builder = new StringBuilder();
        builder.AppendLine($"<h4>Группа: {groupLink}</h4>");
        builder.AppendLine("<table class='table'>");

        List<ProjectFieldInfo> fields;
        bool showContacts = false;

        if (string.IsNullOrWhiteSpace(extra))
        {
            fields = [.. projectInfo.SortedActiveFields];
            showContacts = true;
        }
        else
        {
            fields = [];
            foreach (var r in extra.AsSpan().Split(","))
            {
                var item = extra[r].Trim();
                if (item.Length == 0)
                {
                    continue;
                }
                if (item == "контакты")
                {
                    showContacts = true;
                }
                else if (int.TryParse(item, out var fieldId))
                {
                    fields.Add(projectInfo.GetFieldById(new ProjectFieldIdentification(projectInfo.ProjectId, fieldId)));
                }
            }
        }

        builder.AppendLine("<tr>");
        builder.AppendLine($"<th>Имя&nbsp;персонажа</th>");
        builder.AppendLine("<th>Игрок</th>");

        //TODO: Контакты должны быть колонкой, которую можно поставить в любое место
        //TODO: Добавить колонку «группы» со списком всех группы и публичныегруппы со списком всех публичных групп

        foreach (var field in fields)
        {
            builder.AppendLine($"<th>{field.Name}</th>");
        }
        builder.AppendLine("</tr>");

        foreach (var character in characters)
        {
            var fieldsDict = character.GetFieldsDict(projectInfo);

            builder.AppendLine("<tr>");
            builder.Append($"<td>{CharacterLinkImpl(character)}</td>");
            builder.AppendLine($"<td>{GetPlayerString(character, showContacts)}</td>");

            foreach (var field in fields)
            {
                builder.AppendLine($"<td>{fieldsDict[field.Id].DisplayString}</td>");
            }
            builder.AppendLine("</tr>");
        }
        builder.AppendLine("</table>");
        return builder.ToString();
    }

    private string GroupListFunc(int groupId, string extra, CharacterGroup group, IEnumerable<Character> ch)
    {
        var groupLink = GroupLinkImpl(group, extra);

        var characters = ch.Select(c => CharacterImpl(c));
        var builder = new StringBuilder();
        foreach (var character in characters)
        {
            _ = builder.Append("<br>").Append(character);
        }
        return $"<h4>Группа: {groupLink}</h4><p>{builder}</p>";
    }

    private string GroupListFullFunc(int groupId, string extra, CharacterGroup group, IEnumerable<Character> characters)
    {
        var groupLink = GroupLinkImpl(group, extra);
        var builder = new StringBuilder();
        foreach (var character in characters)
        {
            _ = builder.Append($"<p>&nbsp;<b>{CharacterImpl(character)}</b><br>{character.Description.ToHtmlString()}</p>");
        }
        return $"<h4>Группа: {groupLink}</h4>{group.Description.ToHtmlString()}{builder}<hr>";
    }

    private string GroupName(int groupId, string extra, CharacterGroup group, IEnumerable<Character> ch) => GroupLinkImpl(group, extra);

    private string GroupLinkImpl(CharacterGroup group, ReadOnlySpan<char> extra)
    {
        var name = extra == "" ? group.CharacterGroupName : extra;
        return $"<a href=\"/{Project.ProjectId}/roles/{group.CharacterGroupId}/details\">{name}</a>";
    }

    private string CharacterImpl(Character character, string extra = "")
    {
        return $"<span>{CharacterLinkImpl(character, extra)}&nbsp;({GetPlayerString(character, showContacts: true)})</span>";
    }

    private static string GetPlayerString(Character character, bool showContacts)
    {
        return (character.CharacterType, character.ApprovedClaim?.Player) switch
        {
            (CharacterType.NonPlayer, _) => "NPC",
            (CharacterType.Slot, _) => "шаблон",
            (CharacterType.Player, null) => "нет игрока",
            (CharacterType.Player, User player) when showContacts => GetPlayerContacts(player),
            (CharacterType.Player, User player) when !showContacts => player.GetDisplayName(),
            _ => throw new NotImplementedException(),
        };

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

    private string CharacterLinkImpl(Character character, string extra = "")
    {
        var name = extra == "" ? character.CharacterName : extra;
        return $"<a href=\"/{Project.ProjectId}/character/{character.CharacterId}\">{name}</a>";
    }

    public string Render(string match, int index, string extra)
    {
        if (match.Length > 1 && match[0] == '%' && matches.TryGetValue(match[1..], out Func<string, int, string, string>? func))
        {
            try
            {
                return func(match, index, extra);
            }
            catch (Exception)
            {
                // TODO Need to inject logger here
                return $"ERROR rendering:<pre>{Fail(match, index, extra)}</pre>";
            }
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

    private Func<string, int, string, string> GroupWrapper(Func<int, string, CharacterGroup, IEnumerable<Character>, string> inner)
    {
        return (match, index, extra) =>
        {
            var group = Project.CharacterGroups.SingleOrDefault(c => c.CharacterGroupId == index);
            if (group == null)
            {
                return Fail(match, index, extra);
            }
            IEnumerable<Character> ch = group.GetOrderedCharacters().Where(chr => chr.IsActive)
                    .Union(
                        group.GetOrderedChildrenGroupsRecursive().SelectMany(g => g.GetOrderedCharacters().Where(chr => chr.IsActive))
                        )
                    .Distinct();
            return inner(index, extra, group, ch);
        };
    }

    private Func<string, int, string, string> CharWrapper(Func<Character, string, string> inner)
    {
        return (match, index, extra) =>
        {
            var character = Project.Characters.SingleOrDefault(c => c.CharacterId == index);
            if (character == null)
            {
                return Fail(match, index, extra);
            }
            return inner(character, extra);
        };
    }
}
