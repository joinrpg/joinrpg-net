using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.ProjectCommon;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.CharacterGroups.Test;

/// <summary>
/// Регрессия #5324: в режиме дерева имя персонажа склеивалось со статусом игрока
/// («Хочу на игрушаблон»). Разделитель был задан как <c>&lt;text&gt; &lt;/text&gt;</c>,
/// а статическую разметку из одних пробелов Blazor вырезает.
/// </summary>
public class ProjectRoleGridTreeWhitespaceTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly CharacterIdentification CharacterId = new(ProjectId, 7);
    private static readonly CharacterGroupIdentification RootGroupId = new(ProjectId, 2);

    private sealed class FakeCharacterUriLocator : ICharacterUriLocator
    {
        public Uri GetDetailsUri(CharacterIdentification characterId) => new("https://example.org/character");
        public Uri GetAddClaimUri(CharacterIdentification characterId) => new("https://example.org/apply");
        public Uri GetEditUri(CharacterIdentification characterId) => new("https://example.org/edit");
    }

    private sealed class FakeCharacterLinkUriLocator : IUriLocator<CharacterLinkSlimViewModel>
    {
        public Uri GetUri(CharacterLinkSlimViewModel target) => new("https://example.org/character");
    }

    private sealed class FakeClaimUriLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new("https://example.org/claim");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<ICharacterUriLocator>(new FakeCharacterUriLocator());
        ctx.Services.AddSingleton<IUriLocator<CharacterLinkSlimViewModel>>(new FakeCharacterLinkUriLocator());
        ctx.Services.AddSingleton<IUriLocator<ClaimIdentification>>(new FakeClaimUriLocator());
        return ctx;
    }

    [Fact]
    public void SlotNameIsSeparatedFromPlayerCell()
    {
        using var ctx = CreateContext();

        var charRow = new ProjectRoleGridCharacterRowViewModel(
            new CharacterLinkWithEditViewModel(
                new CharacterLinkSlimViewModel(CharacterId, "Хочу на игру", IsActive: true, ViewMode.Show),
                CanEdit: false,
                ApprovedClaimId: null),
            new PlayerCellViewModel(
                new CharacterApplyViewModel(CharacterId, CharacterBusyStatusView.Slot, SlotCount: null, IsHot: false, IsAvailable: true),
                Contacts: null),
            Groups: null,
            FieldValues: [],
            RootGroupId,
            ActiveClaimsCount: 18);

        var header = new ProjectRoleGridGroupHeaderRowViewModel(
            new CharacterGroupLinkSlimViewModel(RootGroupId, "Все роли", IsPublic: true, IsActive: true),
            DescriptionHtml: null,
            CharacterGroupType.Root);

        var grid = new ProjectRoleGridViewModel(
            RolesListId: null,
            Name: "Все роли",
            CanEditSettings: false,
            HasMasterAccess: false,
            HasPlayerColumn: true,
            HasGroupsColumn: false,
            FieldColumnNames: [],
            Rows: [header, charRow],
            RolesGridGroupsViewMode.Tree,
            RootGroupId,
            CharacterGroupType.Root,
            SuppressFieldLabels: false,
            DefaultClaimProjectStatus: null);

        var root = RenderTreeBuilder.Build(grid.Rows)!;

        var cut = ctx.Render<ProjectRoleGridTreeNode>(p => p
            .Add(x => x.Node, root)
            .Add(x => x.Siblings, [root])
            .Add(x => x.Model, grid));

        // В HTML любая последовательность пробелов — один пробел: важно наличие разделителя.
        var text = string.Join(' ', cut.Find("li").TextContent
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        text.ShouldBe("Хочу на игру шаблон (18 заявок) Заявиться");
    }
}
