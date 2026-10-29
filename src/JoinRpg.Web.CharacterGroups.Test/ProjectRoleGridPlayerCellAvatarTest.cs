using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using JoinRpg.Web.ProjectCommon;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.CharacterGroups.Test;

/// <summary>
/// Аватарки в колонке «Игрок»: настоящая — у открытого игрока, заглушка «?» — у всех остальных строк,
/// чтобы имена в колонке стояли ровно; иконка-человечек рядом с аватаркой не нужна.
/// </summary>
public class ProjectRoleGridPlayerCellAvatarTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly CharacterIdentification CharacterId = new(ProjectId, 7);
    private static readonly CharacterGroupIdentification RootGroupId = new(ProjectId, 2);
    private static readonly Uri AvatarUri = new("https://example.org/avatar.png");

    private sealed class FakeCharacterUriLocator : ICharacterUriLocator
    {
        public Uri GetDetailsUri(CharacterIdentification characterId) => new("https://example.org/character");
        public Uri GetAddClaimUri(CharacterIdentification characterId) => new("https://example.org/apply");
        public Uri GetEditUri(CharacterIdentification characterId) => new("https://example.org/edit");
    }

    private sealed class FakeUserUriLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new("https://example.org/user");
    }

    private static IRenderedComponent<ProjectRoleGridPlayerCell> Render(
        BunitContext ctx,
        PlayerAvatarMode mode,
        CharacterBusyStatusView status,
        UserLinkViewModel? link,
        Uri? avatarUri)
    {
        ctx.Services.AddSingleton<ICharacterUriLocator>(new FakeCharacterUriLocator());
        ctx.Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserUriLocator());

        var player = new PlayerCellViewModel(
            new CharacterApplyViewModel(CharacterId, status, SlotCount: null, IsHot: false, IsAvailable: true),
            Contacts: null,
            link,
            avatarUri);
        var row = new ProjectRoleGridCharacterRowViewModel(
            new CharacterLinkWithEditViewModel(
                new CharacterLinkSlimViewModel(CharacterId, "Вася", IsActive: true, ViewMode.Show),
                CanEdit: false,
                ApprovedClaimId: null),
            player,
            Groups: null,
            FieldValues: [],
            RootGroupId);

        return ctx.Render<ProjectRoleGridPlayerCell>(p => p
            .Add(c => c.Row, row)
            .Add(c => c.Player, player)
            .Add(c => c.AvatarMode, mode));
    }

    private static UserLinkViewModel Player => new(new UserIdentification(5), "Игрок", ViewMode.Show);

    [Theory]
    [InlineData(PlayerAvatarMode.Small)]
    [InlineData(PlayerAvatarMode.Large)]
    public void OpenPlayer_AvatarShownWithoutUserIcon(PlayerAvatarMode mode)
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, mode, CharacterBusyStatusView.HasPlayer, Player, AvatarUri);

        cut.Find("img.role-grid-avatar").GetAttribute("src").ShouldBe(AvatarUri.AbsoluteUri);
        cut.FindAll(".role-grid-avatar-unknown").ShouldBeEmpty();
        cut.FindAll("a.join-user svg").ShouldBeEmpty();
    }

    [Fact]
    public void Vacancy_PlaceholderKeepsColumnAligned()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, PlayerAvatarMode.Large, CharacterBusyStatusView.Vacancy, link: null, avatarUri: null);

        cut.FindAll("img.role-grid-avatar").ShouldBeEmpty();
        cut.Find(".role-grid-avatar-unknown").TextContent.ShouldBe("?");
    }

    [Fact]
    public void AvatarsOff_NoAvatarNoPlaceholder_UserIconKept()
    {
        using var ctx = new BunitContext();

        var cut = Render(ctx, PlayerAvatarMode.None, CharacterBusyStatusView.HasPlayer, Player, AvatarUri);

        cut.FindAll(".role-grid-avatar").ShouldBeEmpty();
        cut.FindAll("a.join-user svg").Count.ShouldBe(1);
    }
}
