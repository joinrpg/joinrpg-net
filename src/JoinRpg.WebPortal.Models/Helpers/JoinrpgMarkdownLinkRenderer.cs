using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Web.Models.UserProfile;
using Markdig.Renderers;

namespace JoinRpg.Web.Models.Helpers;

public class JoinrpgMarkdownLinkRenderer : ILinkRenderer
{
    private Project Project { get; }

    public string[] LinkTypesToMatch { get; }

    private readonly Dictionary<string, RenderFunc> matches;
    private readonly ProjectInfo projectInfo;

    private delegate void RenderFunc(HtmlRenderer renderer, string match, int index, string extra);
    private delegate void CharRenderFunc(HtmlRenderer renderer, Character character, string extra);
    private delegate void CharGroupRenderFunc(HtmlRenderer renderer, CharacterGroup characterGroup, IReadOnlyCollection<Character> characters, string extra);
    private delegate void FieldColumnRenderFunc(HtmlRenderer renderer, Character character, ProjectInfo projectInfo, Dictionary<ProjectFieldIdentification, FieldWithValue> fields);

    public JoinrpgMarkdownLinkRenderer(Project project, ProjectInfo projectInfo)
    {
        Project = project;
        this.projectInfo = projectInfo;
        matches = new Dictionary
          <string, RenderFunc>
        {
          {"%персонаж", CharWrapper(CharacterLinkImpl) },
          {"%контакты", CharWrapper(CharacterImpl) },
          {"%группа", GroupWrapper(GroupName)},
          {"%список", GroupWrapper(GroupListFunc)},
          {"%сеткаролей", GroupWrapper(GroupListFullFunc)},
          {"%экспериментальнаятаблица", GroupWrapper(ExperimentalTableFunc) }
        };

        LinkTypesToMatch = [.. matches.Keys.Select(k => k[1..]).OrderByDescending(c => c.Length)];
    }

    private record class Column(string Name, FieldColumnRenderFunc RenderFunc)
    {
        public static Column Player = new Column("Игрок", (renderer, character, projectInfo, _) => renderer.Write(GetPlayerString(character, projectInfo, showContacts: false)));
        public static Column Contacts = new Column("Игрок", (renderer, character, projectInfo, _) => renderer.Write(GetPlayerString(character, projectInfo, showContacts: true)));
        public static Column FromField(ProjectFieldInfo f) => new Column(f.Name, (renderer, _, _, fieldDict) => renderer.Write(fieldDict[f.Id].DisplayString));



        public static Column Groups = new("Группы", (renderer, character, _, _) => GroupListRenderer(renderer, character, g => true));
        public static Column PublicGroups = new("Группы", (renderer, character, _, _) => GroupListRenderer(renderer, character, g => g.IsPublic));

        private static void GroupListRenderer(HtmlRenderer renderer, Character character, Func<CharacterGroup, bool> groupPredicate)
        {
            var sep = "";
            foreach (var group in character.GetIntrestingGroupsForDisplayToTop().Where(groupPredicate))
            {
                renderer.Write(sep);
                sep = ", ";
                GroupLinkImpl(renderer, group, "");
            }
        }

        public static Column CharacterName = new Column("Персонаж", (renderer, character, _, _) => CharacterLinkImpl(renderer, character, ""));
    }

    private void ExperimentalTableFunc(HtmlRenderer renderer, CharacterGroup group, IReadOnlyCollection<Character> characters, string extra)
    {
        if (!renderer.EnableHtmlForBlock)
        {
            GroupListFullFunc(renderer, group, characters, "");
            return;
        }
        GroupHeader(renderer, group, "");

        renderer.Write("<table class='table'>");
        var columns = SetupColumnsFromExtra(extra);

        renderer.Write("<tr>");

        foreach (var column in columns)
        {
            renderer.Write($"<th>{column.Name}</th>");
        }
        renderer.Write("</tr>");

        foreach (var character in characters)
        {
            var fieldsDict = character.GetFieldsDict(projectInfo);

            renderer.Write("<tr>");

            foreach (var column in columns)
            {
                renderer.Write("<td>");
                column.RenderFunc(renderer, character, projectInfo, fieldsDict);
                renderer.Write("</td>");
            }
            renderer.Write("</tr>");
        }
        renderer.Write("</table>");
    }

    private List<Column> SetupColumnsFromExtra(string extra)
    {
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
                var item = extra.AsSpan(r).Trim();
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

        return columns;
    }

    private void GroupListFunc(HtmlRenderer renderer, CharacterGroup group, IReadOnlyCollection<Character> ch, string extra)
    {
        GroupHeader(renderer, group, extra);
        bool sep = false;

        foreach (var character in ch)
        {
            if (sep)
            {
                if (renderer.EnableHtmlForInline)
                {
                    renderer.Write("<br>");
                }
                else
                {
                    renderer.Write("\n");
                }
            }
            sep = true;
            CharacterImpl(renderer, character, "");
        }
    }

    private void GroupListFullFunc(HtmlRenderer renderer, CharacterGroup group, IReadOnlyCollection<Character> characters, string extra)
    {
        GroupHeader(renderer, group, extra);
        RenderInnerMarkdown(renderer, group.Description);
        foreach (var character in characters)
        {
            if (renderer.EnableHtmlForBlock)
            {
                renderer.Write("<p>&nbsp;");
            }
            else
            {
                renderer.Write(" ");
            }
            if (renderer.EnableHtmlForInline)
            {
                renderer.Write("<b>");
            }
            CharacterImpl(renderer, character, "");
            if (renderer.EnableHtmlForInline)
            {
                renderer.Write("</b>");
            }
            if (renderer.EnableHtmlForBlock)
            {
                renderer.Write("<br>");
            }
            RenderInnerMarkdown(renderer, character.Description);
            if (renderer.EnableHtmlForBlock)
            {
                renderer.Write("</p>");
            }
        }
    }

    private void GroupHeader(HtmlRenderer renderer, CharacterGroup group, string extra)
    {
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("<h4>");
        }
        GroupLinkImpl(renderer, group, extra);
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("</h4>");
        }
    }

    private static void RenderInnerMarkdown(HtmlRenderer renderer, MarkdownString markdownString)
    {
        if (renderer.EnableHtmlForBlock)
        {
            renderer.Write(markdownString.ToHtmlString().Value);
        }
        else
        {
            renderer.Write(markdownString.ToPlainTextAndEscapeHtml().Value);
        }
    }

    private void GroupName(HtmlRenderer renderer, CharacterGroup characterGroup, IReadOnlyCollection<Character> characters, string extra) => GroupLinkImpl(renderer, characterGroup, extra);

    private static void GroupLinkImpl(HtmlRenderer renderer, CharacterGroup group, string extra)
    {
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("<a href=\"/{group.ProjectId}/roles/{group.CharacterGroupId}/details\">");
        }
        renderer.Write(extra == "" ? group.CharacterGroupName : extra);
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("</a>");
        }
    }

    private void CharacterImpl(HtmlRenderer renderer, Character character, string extra)
    {
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("<span>");
        }
        CharacterLinkImpl(renderer, character, extra);
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("&nbsp;(");
        }
        else
        {
            renderer.Write(" (");
        }
        renderer.Write(GetPlayerString(character, projectInfo, showContacts: true));
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write(")</span>");
        }
    }

    private static string GetPlayerString(Character character, ProjectInfo projectInfo, bool showContacts)
    {
        if (projectInfo.ProjectStatus == ProjectLifecycleStatus.Archived)
        {
            // Для архивных проектов не показывать контакты
            showContacts = false;
        }
        return (character.CharacterType, character.ApprovedClaim?.Player) switch
        {
            (CharacterType.NonPlayer, _) => "NPC",
            (CharacterType.Slot, _) => "шаблон",
            (CharacterType.Player, null) => "нет игрока",
            (CharacterType.Player, User player) when showContacts => GetPlayerContacts(player),
            (CharacterType.Player, User player) when !showContacts => player.GetDisplayName(),
            _ => throw new NotImplementedException(),
        };
    }

    internal static string GetPlayerContacts(User player)
    {
        static string? GetContactLink(string contactName, Link? link)
        {
            if (link is null)
            {
                return null;
            }
            return $"{contactName}: <a href=\"{link.Uri.AbsoluteUri}\">{link.Label}</a>";
        }

        string?[] contacts = [
                GetContactLink("Email", UserSocialLink.GetEmailUri(player.Email)),
                GetContactLink("ВК", UserSocialLink.GetVKUri(player.Extra?.Vk)),
                GetContactLink("Телеграм", UserSocialLink.GetTelegramUri(player.Extra?.Telegram))
                ];

        return $"{player.GetDisplayName()} ({contacts.JoinIfNotNullOrWhitespace(", ")})";
    }

    private static void CharacterLinkImpl(HtmlRenderer renderer, Character character, string extra)
    {
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write($"<a href =\"/{character.ProjectId}/character/{character.CharacterId}\">");
        }
        renderer.Write(extra == "" ? character.CharacterName : extra);
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write("</a>");
        }
    }

    public void Render(HtmlRenderer renderer, string match, int index, string extra)
    {
        RenderFunc func;
        if (match.Length > 1 && match[0] == '%')
        {
            func = matches.GetValueOrDefault(match, Fail);
        }
        else
        {
            func = Fail;
        }
        try
        {
            func(renderer, match, index, extra);
        }
        catch (Exception)
        {
            // TODO Need to inject logger here
            renderer.Write($"ERROR rendering:");
            if (renderer.EnableHtmlForInline)
            {
                renderer.Write("<pre>");
            }
            Fail(renderer, match, index, extra);
            if (renderer.EnableHtmlForInline)
            {
                renderer.Write("</pre>");
            }
        }
    }

    private static void Fail(HtmlRenderer renderer, string match, int index, string extra)
    {
        if (!string.IsNullOrEmpty(extra))
        {
            extra = $"({extra})";
        }
        renderer.Write($"{match}{index}{extra}");
    }

    private RenderFunc GroupWrapper(CharGroupRenderFunc inner)
    {
        return (renderer, match, index, extra) =>
        {
            var group = Project.CharacterGroups.SingleOrDefault(c => c.CharacterGroupId == index);
            if (group == null)
            {
                Fail(renderer, match, index, extra);
                return;
            }
            IReadOnlyCollection<Character> ch = [
                ..group.GetOrderedCharacters().Where(chr => chr.IsActive)
                    .Union(
                        group.GetOrderedChildrenGroupsRecursive().SelectMany(g => g.GetOrderedCharacters().Where(chr => chr.IsActive))
                        )
                    .Distinct()
                    ];
            inner(renderer, group, ch, extra);
        };
    }

    private RenderFunc CharWrapper(CharRenderFunc inner)
    {
        return (renderer, match, index, extra) =>
        {
            var character = Project.Characters.SingleOrDefault(c => c.CharacterId == index);
            if (character == null)
            {
                Fail(renderer, match, index, extra);
            }
            else
            {
                inner(renderer, character, extra);
            }
        };
    }
}
