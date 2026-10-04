using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

public class ListOperationsButtonsTest
{
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly CharacterGroupIdentification GroupId = new(ProjectId, 5);

    private sealed class FakeLocator : IProjectUriLocator, ICharacterGroupUriLocator
    {
        private static Uri UriFor(string suffix) => new($"https://example.org/{suffix}");

        public Uri GetMyClaimUri(ProjectIdentification projectId) => UriFor("my-claim");
        public Uri GetAddClaimUri(ProjectIdentification projectId) => UriFor("add-claim");
        public Uri GetCreatePlotUri(ProjectIdentification projectId) => UriFor("create-plot");
        public Uri GetRolesListUri(ProjectIdentification projectId) => UriFor("roles");
        public Uri GetCaptainCabinetUri(ProjectIdentification projectId) => UriFor("captain-cabinet");
        public Uri GetCreateCharacterUri(ProjectIdentification projectId) => UriFor("create-character-in-project");
        public Uri GetMassMailUri(ProjectIdentification projectId, IReadOnlyCollection<ClaimIdentification> claimIds)
            => UriFor($"mass-mail/{claimIds.Count}");
        public Uri GetPrintCharactersUri(ProjectIdentification projectId, IReadOnlyCollection<CharacterIdentification> characterIds)
            => UriFor($"print/{characterIds.Count}");

        public Uri GetClaimListUri(CharacterGroupIdentification groupId) => UriFor("claims");
        public Uri GetDiscussingClaimListUri(CharacterGroupIdentification groupId) => UriFor("discussing");
        public Uri GetCharacterListUri(CharacterGroupIdentification groupId) => UriFor("characters");
        public Uri GetReportUri(CharacterGroupIdentification groupId) => UriFor("report");
        public Uri GetSubscribeUri(CharacterGroupIdentification groupId) => UriFor("subscribe");
        public Uri GetEditUri(CharacterGroupIdentification groupId) => UriFor("edit");
        public Uri GetDeleteUri(CharacterGroupIdentification groupId) => UriFor("delete");
        public Uri GetCreateCharacterUri(CharacterGroupIdentification groupId) => UriFor($"create-character-in-group/{groupId.CharacterGroupId}");
        public Uri GetAddGroupUri(CharacterGroupIdentification groupId) => UriFor("add-group");
    }

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        var locator = new FakeLocator();
        ctx.Services.AddSingleton<IProjectUriLocator>(locator);
        ctx.Services.AddSingleton<ICharacterGroupUriLocator>(locator);
        ctx.Services.AddLogging();
        return ctx;
    }

    private static string[] Links(IRenderedComponent<ListOperationsButtons> cut)
        => [.. cut.FindAll("a").Select(a => a.GetAttribute("href")!)];

    [Fact]
    public void WithoutProject_OnlyCountAndExport()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<ListOperationsButtons>(p => p
            .Add(x => x.ExportUri, "/list?export=xlsx")
            .Add(x => x.CountString, "Всего: 3")
            .Add(x => x.ShowCreateCharacter, true)
            .Add(x => x.ClaimIds, [new ClaimIdentification(ProjectId, 1)]));

        Links(cut).ShouldBe(["/list?export=xlsx"]);
        var count = cut.Find("button");
        count.HasAttribute("disabled").ShouldBeTrue();
        count.TextContent.ShouldContain("Всего: 3");
    }

    [Fact]
    public void CreateCharacter_InGroup_WhenGroupGiven()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<ListOperationsButtons>(p => p
            .Add(x => x.ExportUri, "/export")
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.CharacterGroupId, GroupId)
            .Add(x => x.ShowCreateCharacter, true));

        Links(cut).ShouldBe(["/export", "https://example.org/create-character-in-group/5"]);
    }

    [Fact]
    public void CreateCharacter_InProject_WithoutGroup()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<ListOperationsButtons>(p => p
            .Add(x => x.ExportUri, "/export")
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ShowCreateCharacter, true));

        Links(cut).ShouldBe(["/export", "https://example.org/create-character-in-project"]);
    }

    /// <summary>Так у списков заявок по группе: группа есть, а создавать персонажа оттуда не предлагаем.</summary>
    [Fact]
    public void CreateCharacter_Hidden_WhenNotRequested_EvenWithGroup()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<ListOperationsButtons>(p => p
            .Add(x => x.ExportUri, "/export")
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.CharacterGroupId, GroupId)
            .Add(x => x.ShowCreateCharacter, false));

        Links(cut).ShouldBe(["/export"]);
    }

    [Fact]
    public void MassMail_OnlyForNonEmptyClaims()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<ListOperationsButtons>(p => p
            .Add(x => x.ExportUri, "/export")
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ClaimIds, [new ClaimIdentification(ProjectId, 1), new ClaimIdentification(ProjectId, 2)]));

        Links(cut).ShouldBe(["/export", "https://example.org/mass-mail/2"]);
    }

    [Fact]
    public void Print_ForCharacters()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<ListOperationsButtons>(p => p
            .Add(x => x.ExportUri, "/export")
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.CharacterIds, [new CharacterIdentification(ProjectId, 7)]));

        Links(cut).ShouldBe(["/export", "https://example.org/print/1"]);
    }
}
