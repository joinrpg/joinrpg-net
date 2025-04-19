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

    private record class Column(string Name, Func<Character, Dictionary<ProjectFieldIdentification, FieldWithValue>, string> Getter)
    {
        public static Column Player = new Column("Игрок", (character, fieldDict) => GetPlayerString(character, showContacts: false));
        public static Column Contacts = new Column("Игрок", (character, fieldDict) => GetPlayerString(character, showContacts: true));
        public static Column FromField(ProjectFieldInfo f) => new Column(f.Name, (character, fieldDict) => fieldDict[f.Id].DisplayString);



        public static Column Groups =
            new("Группы", (character, _) => string.Join(", ", character.GetIntrestingGroupsForDisplayToTop().Select(g => GroupLinkImpl(g, ""))));
        public static Column PublicGroups =
            new("Группы", (character, _) => string.Join(", ", character.GetIntrestingGroupsForDisplayToTop().Where(g => g.IsPublic).Select(g => GroupLinkImpl(g, ""))));

        public static Column CharacterName = new Column("Персонаж", (character, _) => CharacterLinkImpl(character));
    }

    private string ExperimentalTableFunc(int groupId, string extra, CharacterGroup group, IEnumerable<Character> characters)
    {
        var groupLink = GroupLinkImpl(group, "");

        var builder = new StringBuilder();
        builder.AppendLine($"<h4>Группа: {groupLink}</h4>");
        builder.AppendLine("<table class='table'>");

        List<Column> columns = [];

        columns.Add(Column.CharacterName);

        if (string.IsNullOrWhiteSpace(extra))
        {
            columns.Add(Column.Player);
            columns.AddRange(projectInfo.SortedActiveFields.Select(Column.FromField));
        }
        else
        {
            foreach (var r in extra.AsSpan().Split(","))
            {
                var item = extra[r].Trim();
                if (item.Length == 0)
                {
                    continue;
                }
                if (item.Equals("контакты", StringComparison.InvariantCultureIgnoreCase))
                {
                    columns.Add(Column.Contacts);
                }
                else if (item.Equals("игрок", StringComparison.InvariantCultureIgnoreCase))
                {
                    columns.Add(Column.Player);
                }
                else if (item.Equals("группы", StringComparison.InvariantCultureIgnoreCase))
                {
                    columns.Add(Column.Groups);
                }
                else if (item.Equals("публичныегруппы", StringComparison.InvariantCultureIgnoreCase))
                {
                    columns.Add(Column.PublicGroups);
                }
                else if (int.TryParse(item, out var fieldId))
                {
                    var field = projectInfo.GetFieldById(new ProjectFieldIdentification(projectInfo.ProjectId, fieldId));
                    columns.Add(Column.FromField(field));
                }
            }
        }

        builder.AppendLine("<tr>");

        foreach (var column in columns)
        {
            builder.AppendLine($"<th>{column.Name}</th>");
        }
        builder.AppendLine("</tr>");

        foreach (var character in characters)
        {
            var fieldsDict = character.GetFieldsDict(projectInfo);

            builder.AppendLine("<tr>");

            foreach (var column in columns)
            {
                builder.AppendLine($"<td>{column.Getter(character, fieldsDict)}</td>");
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

    private static string GroupLinkImpl(CharacterGroup group, ReadOnlySpan<char> extra)
    {
        var name = extra == "" ? group.CharacterGroupName : extra;
        return $"<a href=\"/{group.ProjectId}/roles/{group.CharacterGroupId}/details\">{name}</a>";
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

    private static string CharacterLinkImpl(Character character, string extra = "")
    {
        var name = extra == "" ? character.CharacterName : extra;
        return $"<a href=\"/{character.ProjectId}/character/{character.CharacterId}\">{name}</a>";
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
