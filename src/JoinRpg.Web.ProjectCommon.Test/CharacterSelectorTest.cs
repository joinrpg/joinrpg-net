using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// Регрессия: <see cref="CharacterSelector"/> ходил в API на каждое выставление параметров,
/// то есть на каждую перерисовку родителя (в частности — на каждое нажатие клавиши в соседнем поле формы).
/// </summary>
public class CharacterSelectorTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private sealed class CountingCharactersClient : ICharactersClient
    {
        public List<(ProjectIdentification ProjectId, CharacterListType ListType)> Calls { get; } = [];

        public Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType = CharacterListType.All)
        {
            Calls.Add((projectId, listType));
            return Task.FromResult<List<CharacterDto>>(
            [
                new CharacterDto(new CharacterIdentification(projectId, 10), "Первый", "", IsPublic: true),
                new CharacterDto(new CharacterIdentification(projectId, 20), "Второй", "", IsPublic: true),
            ]);
        }
    }

    private static (BunitContext Context, CountingCharactersClient Client) CreateContext()
    {
        var ctx = new BunitContext();
        var client = new CountingCharactersClient();
        ctx.Services.AddSingleton<ICharactersClient>(client);
        // Non-interactive: TypedSelector не пытается импортировать JS-модуль bootstrap-select.
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return (ctx, client);
    }

    [Fact]
    public void RerenderWithSameParameters_DoesNotRequestCharactersAgain()
    {
        var (ctx, client) = CreateContext();
        using var _ = ctx;

        var cut = ctx.Render<CharacterSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ListType, CharacterListType.All));

        client.Calls.Count.ShouldBe(1);

        // Родитель перерисовался, параметры те же — запроса быть не должно.
        cut.Render(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ListType, CharacterListType.All));

        client.Calls.Count.ShouldBe(1);
    }

    [Fact]
    public void ChangingListType_RequestsCharactersAgain()
    {
        var (ctx, client) = CreateContext();
        using var _ = ctx;

        var cut = ctx.Render<CharacterSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ListType, CharacterListType.All));

        cut.Render(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ListType, CharacterListType.AvailableForMaster));

        client.Calls.Count.ShouldBe(2);
        client.Calls[1].ListType.ShouldBe(CharacterListType.AvailableForMaster);
    }

    [Fact]
    public void ChangingProjectId_RequestsCharactersAgain()
    {
        var (ctx, client) = CreateContext();
        using var _ = ctx;

        var cut = ctx.Render<CharacterSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.Render(p => p.Add(x => x.ProjectId, new ProjectIdentification(2)));

        client.Calls.Count.ShouldBe(2);
    }

    [Fact]
    public void ChangingExcludedIds_FiltersLocallyWithoutNewRequest()
    {
        var (ctx, client) = CreateContext();
        using var _ = ctx;

        var cut = ctx.Render<CharacterSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        cut.Markup.ShouldContain("Первый");
        cut.Markup.ShouldContain("Второй");

        cut.Render(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.ExcludeCharacterIds, [new CharacterIdentification(ProjectId, 10)]));

        client.Calls.Count.ShouldBe(1);
        cut.Markup.ShouldNotContain("Первый");
        cut.Markup.ShouldContain("Второй");
    }
}
