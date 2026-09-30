using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// <see cref="CharacterGroupSelector"/> рендерит <c>&lt;select&gt;</c>, который в MVC-формах постится
/// напрямую в модель (<c>param-Name</c>), поэтому в тестах проверяется именно формат value у опций:
/// полный идентификатор группы, который понимает ProjectEntityIdModelBinder.
/// </summary>
public class CharacterGroupSelectorTest
{
    private const int ProjectId = 1;
    private static readonly CharacterGroupIdentification FirstGroupId = new(ProjectId, 10);
    private static readonly CharacterGroupIdentification SecondGroupId = new(ProjectId, 20);

    private sealed class FakeCharacterGroupsClient : ICharacterGroupsClient
    {
        public int CallCount { get; private set; }

        private Task<List<CharacterGroupDto>> Groups()
        {
            CallCount++;
            return Task.FromResult<List<CharacterGroupDto>>(
            [
                new(FirstGroupId, "Первая", ["Корень", "Первая"], IsPublic: true, IsSpecial: false),
                new(SecondGroupId, "Вторая", ["Корень", "Вторая"], IsPublic: true, IsSpecial: false),
            ]);
        }

        public Task<List<CharacterGroupDto>> GetRealCharacterGroups(int projectId) => Groups();

        public Task<List<CharacterGroupDto>> GetCharacterGroupsWithSpecial(int projectId) => Groups();

        public Task<List<CharacterGroupDto>> GetValidParentGroups(CharacterGroupIdentification groupId) => Groups();
    }

    private static (BunitContext Context, FakeCharacterGroupsClient Client) CreateContext()
    {
        var ctx = new BunitContext();
        var client = new FakeCharacterGroupsClient();
        ctx.Services.AddSingleton<ICharacterGroupsClient>(client);
        // Non-interactive: TypedSelector не пытается импортировать JS-модуль bootstrap-select.
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return (ctx, client);
    }

    [Fact]
    public void OptionValues_AreFullTypedIds()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        var values = cut.FindAll("option").Select(o => o.GetAttribute("value")).ToArray();
        values.ShouldBe([FirstGroupId.ToString(), SecondGroupId.ToString()]);
    }

    [Fact]
    public void SelectedGroupIds_MarkOptionSelected()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedGroupIds, [SecondGroupId]));

        cut.Find($"option[value='{SecondGroupId}']").HasAttribute("selected").ShouldBeTrue();
        cut.Find($"option[value='{FirstGroupId}']").HasAttribute("selected").ShouldBeFalse();
    }

    [Fact]
    public void Name_IsRenderedOnSelect_SoMvcFormCanPostIt()
    {
        var (ctx, _) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.Name, "ParentCharacterGroupIds"));

        cut.Find("select").GetAttribute("name").ShouldBe("ParentCharacterGroupIds");
    }

    [Fact]
    public void RerenderWithSameParameters_DoesNotRequestGroupsAgain()
    {
        var (ctx, client) = CreateContext();
        using var _unused = ctx;

        var cut = ctx.Render<CharacterGroupSelector>(p => p.Add(x => x.ProjectId, ProjectId));
        client.CallCount.ShouldBe(1);

        cut.Render(p => p.Add(x => x.ProjectId, ProjectId));

        client.CallCount.ShouldBe(1);
    }
}
