using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.CharacterGroups.Test;

/// <summary>
/// #5655: в заблокированном проекте группы править можно, а удалять группу и добавлять
/// персонажа — нет (ADR023). Меню группы в сетке ролей должно различать эти права.
/// </summary>
public class GroupEditMenuTest
{
    private static readonly CharacterGroupIdentification GroupId = new(new ProjectIdentification(1), 2);

    private sealed class FakeCharacterGroupUriLocator : ICharacterGroupUriLocator
    {
        public Uri GetClaimListUri(CharacterGroupIdentification groupId) => new("https://example.org/claims");
        public Uri GetDiscussingClaimListUri(CharacterGroupIdentification groupId) => new("https://example.org/discussing");
        public Uri GetCharacterListUri(CharacterGroupIdentification groupId) => new("https://example.org/characters");
        public Uri GetReportUri(CharacterGroupIdentification groupId) => new("https://example.org/report");
        public Uri GetSubscribeUri(CharacterGroupIdentification groupId) => new("https://example.org/subscribe");
        public Uri GetEditUri(CharacterGroupIdentification groupId) => new("https://example.org/edit");
        public Uri GetDeleteUri(CharacterGroupIdentification groupId) => new("https://example.org/delete");
        public Uri GetCreateCharacterUri(CharacterGroupIdentification groupId) => new("https://example.org/create-character");
        public Uri GetAddGroupUri(CharacterGroupIdentification groupId) => new("https://example.org/add-group");
    }

    private static IReadOnlyList<string> RenderEditLinks(bool showEditGroupControls, bool showEditRoleControls)
    {
        using var ctx = new BunitContext();
        ctx.Services.AddSingleton<ICharacterGroupUriLocator>(new FakeCharacterGroupUriLocator());

        var cut = ctx.Render<GroupEditMenu.GroupEditMenu>(p => p
            .Add(x => x.GroupId, GroupId)
            .Add(x => x.GroupType, CharacterGroupType.Regular)
            .Add(x => x.HasMasterAccess, true)
            .Add(x => x.ShowEditGroupControls, showEditGroupControls)
            .Add(x => x.ShowEditRoleControls, showEditRoleControls));

        string[] editLinks = ["edit", "delete", "create-character", "add-group"];
        return [.. cut.FindAll("a")
            .Select(a => a.GetAttribute("href")!.Replace("https://example.org/", ""))
            .Where(editLinks.Contains)];
    }

    [Fact]
    public void ActiveProject_AllEditLinks()
        => RenderEditLinks(showEditGroupControls: true, showEditRoleControls: true)
            .ShouldBe(["edit", "delete", "create-character", "add-group"]);

    [Fact]
    public void BlockedProject_NoDeleteAndNoAddCharacter()
        => RenderEditLinks(showEditGroupControls: true, showEditRoleControls: false)
            .ShouldBe(["edit", "add-group"]);

    [Fact]
    public void ArchivedProject_NoEditLinks()
        => RenderEditLinks(showEditGroupControls: false, showEditRoleControls: false)
            .ShouldBeEmpty();
}
