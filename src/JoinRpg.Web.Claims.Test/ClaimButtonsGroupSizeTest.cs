using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.Claims.CaptainCabinet;
using JoinRpg.Web.ProjectCommon.Claims;
using JoinRpg.Web.ProjectCommon.Projects;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Обёртки над <see cref="JoinButton"/> без явного размера берут размер окружающей
/// <see cref="JoinButtonGroup"/>, а вне группы остаются обычного размера (#5305).
/// </summary>
public class ClaimButtonsGroupSizeTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddLogging();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IProjectUriLocator>(new FakeProjectUriLocator());
        ctx.Services.AddSingleton<IUriLocator<ClaimIdentification>>(new FakeClaimUriLocator());
        return ctx;
    }

    public static TheoryData<string> Buttons => [nameof(ClaimButton), nameof(SendClaimButton), nameof(MyClaimButton), nameof(CaptainCabinetButton)];

    private static readonly ClaimLinkViewModel Claim = new(
        new ClaimIdentification(ProjectId, 2), new UserDisplayName("Игрок", null), "Персонаж", "", new UserIdentification(3));

    private static RenderFragment Button(string name, SizeStyleEnum? size) => name switch
    {
        nameof(ClaimButton) => Component<ClaimButton>(size, (nameof(ClaimButton.Claim), Claim)),
        nameof(SendClaimButton) => Component<SendClaimButton>(size,
            (nameof(SendClaimButton.ProjectId), ProjectId),
            (nameof(SendClaimButton.ProjectStatus), ProjectLifecycleStatus.ActiveClaimsOpen)),
        nameof(MyClaimButton) => Component<MyClaimButton>(size,
            (nameof(MyClaimButton.ProjectId), ProjectId),
            (nameof(MyClaimButton.ProjectStatus), ProjectLifecycleStatus.ActiveClaimsOpen)),
        nameof(CaptainCabinetButton) => Component<CaptainCabinetButton>(size, (nameof(CaptainCabinetButton.ProjectId), ProjectId)),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    private static RenderFragment Component<TComponent>(SizeStyleEnum? size, params (string Name, object Value)[] parameters)
        where TComponent : IComponent => b =>
        {
            b.OpenComponent<TComponent>(0);
            foreach (var (paramName, value) in parameters)
            {
                b.AddAttribute(1, paramName, value);
            }
            b.AddAttribute(2, "Size", size);
            b.CloseComponent();
        };

    private static string[] RenderInGroup(string name, SizeStyleEnum? size)
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButtonGroup>(p => p
            .Add(x => x.Size, SizeStyleEnum.Small)
            .AddChildContent(Button(name, size)));
        return (cut.Find(".join-btn").GetAttribute("class") ?? "").Split(' ');
    }

    [Theory]
    [MemberData(nameof(Buttons))]
    public void WithoutOwnSize_TakesGroupSize(string name)
        => RenderInGroup(name, size: null).ShouldContain("join-btn--sm");

    [Theory]
    [MemberData(nameof(Buttons))]
    public void OwnSize_WinsOverGroup(string name)
    {
        var classes = RenderInGroup(name, SizeStyleEnum.Large);
        classes.ShouldContain("join-btn--lg");
        classes.ShouldNotContain("join-btn--sm");
    }

    [Theory]
    [MemberData(nameof(Buttons))]
    public void OutsideGroup_HasNoSizeClass(string name)
    {
        using var ctx = CreateContext();
        var cut = ctx.Render(Button(name, size: null));

        cut.Find(".join-btn").ClassList.ShouldAllBe(c => c != "join-btn--sm" && c != "join-btn--lg" && c != "join-btn--xs");
    }

    private sealed class FakeClaimUriLocator : IUriLocator<ClaimIdentification>
    {
        public Uri GetUri(ClaimIdentification target) => new($"https://example.org/claims/{target.ClaimId}");
    }
}
