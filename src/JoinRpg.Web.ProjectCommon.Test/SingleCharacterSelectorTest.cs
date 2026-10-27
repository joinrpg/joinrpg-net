using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// Пункт «новый персонаж» в <see cref="SingleCharacterSelector"/>: по умолчанию его нет,
/// а когда он включён, его выбор означает <see cref="SingleCharacterSelector.SelectedCharacterId"/> == null.
/// </summary>
public class SingleCharacterSelectorTest
{
    private const string NewCharacterText = "Новый персонаж";
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly CharacterIdentification FirstId = new(ProjectId, 10);

    private sealed class StubCharactersClient : ICharactersClient
    {
        public Task<List<CharacterDto>> GetCharacters(ProjectIdentification projectId, CharacterListType listType = CharacterListType.All)
            => Task.FromResult<List<CharacterDto>>(
            [
                new CharacterDto(new CharacterIdentification(projectId, 10), "Первый", "", IsPublic: true),
                new CharacterDto(new CharacterIdentification(projectId, 20), "Второй", "", IsPublic: true),
            ]);
    }

    private static BunitContext CreateStaticContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<ICharactersClient>(new StubCharactersClient());
        // Non-interactive: так TypedSelector рендерится в MVC-формах (render-mode="Static").
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return ctx;
    }

    private static BunitContext CreateInteractiveContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<ICharactersClient>(new StubCharactersClient());
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        return ctx;
    }

    [Fact]
    public void ByDefault_NoNewCharacterOption()
    {
        using var ctx = CreateStaticContext();

        var cut = ctx.Render<SingleCharacterSelector>(p => p.Add(x => x.ProjectId, ProjectId));

        var options = cut.FindAll(".join-select__list input");
        options.Count.ShouldBe(2);
        cut.Markup.ShouldNotContain(NewCharacterText);
    }

    [Fact]
    public void WithText_NewCharacterOptionGoesFirstAndIsSelectedWhenNoCharacter()
    {
        using var ctx = CreateStaticContext();

        var cut = ctx.Render<SingleCharacterSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.NewCharacterOptionText, NewCharacterText));

        var options = cut.FindAll(".join-select__list input");
        options.Count.ShouldBe(3);
        cut.FindAll(".join-select__list .join-select__text")[0].TextContent.ShouldBe(NewCharacterText);
        options[0].GetAttribute("value").ShouldBe("");
        options[0].HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void WithTextAndSelectedCharacter_NewCharacterOptionIsNotSelected()
    {
        using var ctx = CreateStaticContext();

        var cut = ctx.Render<SingleCharacterSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedCharacterId, FirstId)
            .Add(x => x.NewCharacterOptionText, NewCharacterText));

        var options = cut.FindAll(".join-select__list input");
        options[0].HasAttribute("checked").ShouldBeFalse();
        options[1].HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void ChoosingNewCharacterThenCharacter_ReportsNullThenId()
    {
        using var ctx = CreateInteractiveContext();
        var reported = new List<CharacterIdentification?>();

        var cut = ctx.Render<SingleCharacterSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedCharacterId, FirstId)
            .Add(x => x.SelectedCharacterIdChanged, (CharacterIdentification? id) => reported.Add(id))
            .Add(x => x.NewCharacterOptionText, NewCharacterText));
        var firstValue = cut.FindAll(".join-select__list input")[1].GetAttribute("value")!;

        cut.Find(".join-select__list input[value='']").Change(true);

        reported.ShouldBe([null]);

        cut.Find($"input[value='{firstValue}']").Change(true);

        reported.ShouldBe([null, FirstId]);
    }
}
