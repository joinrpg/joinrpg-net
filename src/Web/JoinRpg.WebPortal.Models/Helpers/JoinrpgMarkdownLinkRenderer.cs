using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Helpers;
using JoinRpg.Markdown;
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
    private delegate void CharGroupRenderFunc(HtmlRenderer renderer, CharacterGroupInfo characterGroup, IReadOnlyCollection<Character> characters, string extra);

    /// <remarks>
    /// Создаётся только через <see cref="JoinrpgMarkdownLinkRendererFactory"/>: выбор способа
    /// загрузки данных живёт там, а не у вызывающих.
    /// </remarks>
    internal JoinrpgMarkdownLinkRenderer(Project project, ProjectInfo projectInfo)
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
          {"%сеткаролей", NotSupported},
          {"%экспериментальнаятаблица", NotSupported }
        };

        LinkTypesToMatch = [.. matches.Keys.Select(k => k[1..]).OrderByDescending(c => c.Length)];
    }

    private void GroupListFunc(HtmlRenderer renderer, CharacterGroupInfo group, IReadOnlyCollection<Character> ch, string extra)
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

    private void GroupHeader(HtmlRenderer renderer, CharacterGroupInfo group, string extra)
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

    private void GroupName(HtmlRenderer renderer, CharacterGroupInfo characterGroup, IReadOnlyCollection<Character> characters, string extra) => GroupLinkImpl(renderer, characterGroup, extra);

    private static void GroupLinkImpl(HtmlRenderer renderer, CharacterGroupInfo group, string extra)
    {
        if (renderer.EnableHtmlForInline)
        {
            renderer.Write($"<a href=\"/{group.Id.ProjectId.Id}/roles/{group.Id.CharacterGroupId}/details\">");
        }
        renderer.Write(extra == "" ? group.Name : extra);
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

    private static void NotSupported(HtmlRenderer renderer, string match, int index, string extra)
    {
        Fail(renderer, match, index, extra);
        renderer.Write(" больше не поддерживается. Создайте тип вводной «Сетка ролей» для аналогичной функциональности");
    }

    private RenderFunc GroupWrapper(CharGroupRenderFunc inner)
    {
        return (renderer, match, index, extra) =>
        {
            CharacterGroupInfo group = projectInfo.GetGroupById(index);
            if (group == null)
            {
                Fail(renderer, match, index, extra);
                return;
            }

            var groupsIds = group.AllChildGroupsIncludingThis;

            IReadOnlyCollection<Character> ch = [
                ..Project.CharacterGroups.Where(g => groupsIds.Contains(g.GetId())).SelectMany(g =>  g.GetOrderedCharacters().Where(chr => chr.IsActive)).Distinct()
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
