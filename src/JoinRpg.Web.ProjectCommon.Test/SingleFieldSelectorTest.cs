using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Web.ProjectCommon.Fields;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JoinRpg.Web.ProjectCommon.Test;

/// <summary>
/// <see cref="SingleFieldSelector"/> показывает только поля нужного типа и привязки,
/// а пустой пункт означает <see cref="SingleFieldSelector.SelectedFieldId"/> == null.
/// </summary>
public class SingleFieldSelectorTest
{
    private const string EmptyText = "Нет поля";
    private static readonly ProjectIdentification ProjectId = new(1);
    private static readonly ProjectFieldIdentification CharacterStringId = new(ProjectId, 10);

    private sealed class StubFieldsClient : IProjectFieldsClient
    {
        public Task<List<ProjectFieldDto>> GetProjectFields(ProjectIdentification projectId)
            => Task.FromResult<List<ProjectFieldDto>>(
            [
                new ProjectFieldDto(new(projectId, 10), "Строка персонажа", ProjectFieldType.String, FieldBoundTo.Character),
                new ProjectFieldDto(new(projectId, 20), "Текст персонажа", ProjectFieldType.Text, FieldBoundTo.Character),
                new ProjectFieldDto(new(projectId, 30), "Строка заявки", ProjectFieldType.String, FieldBoundTo.Claim),
            ]);
    }

    private static BunitContext CreateStaticContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<IProjectFieldsClient>(new StubFieldsClient());
        _ = ctx.Services.AddJoinRpgWebComponents();
        _ = ctx.AddBunitPersistentComponentState();
        // Non-interactive: так TypedSelector рендерится в MVC-формах (render-mode="Static").
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return ctx;
    }

    [Fact]
    public void ShowsOnlyFieldsOfRequestedTypeAndBinding()
    {
        using var ctx = CreateStaticContext();

        var cut = ctx.Render<SingleFieldSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.FieldType, ProjectFieldType.String)
            .Add(x => x.BoundTo, FieldBoundTo.Character));

        cut.FindAll(".join-select__list .join-select__text").Select(o => o.TextContent).ShouldBe(["Строка персонажа"]);
        // Одиночный выбор — radio, а не checkbox.
        cut.FindAll(".join-select__list input").Select(i => i.GetAttribute("type")).ShouldBe(["radio"]);
    }

    [Fact]
    public void WithoutSelection_EmptyOptionIsSelected()
    {
        using var ctx = CreateStaticContext();

        var cut = ctx.Render<SingleFieldSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.AllowEmpty, true)
            .Add(x => x.EmptyText, EmptyText));

        cut.FindAll(".join-select__list .join-select__text")[0].TextContent.ShouldBe(EmptyText);
        cut.FindAll(".join-select__list input")[0].HasAttribute("checked").ShouldBeTrue();
    }

    [Fact]
    public void WithSelection_SelectedFieldIsSelected()
    {
        using var ctx = CreateStaticContext();

        var cut = ctx.Render<SingleFieldSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.SelectedFieldId, CharacterStringId)
            .Add(x => x.AllowEmpty, true)
            .Add(x => x.EmptyText, EmptyText));

        var options = cut.FindAll(".join-select__list input");
        options[0].HasAttribute("checked").ShouldBeFalse();
        options[1].HasAttribute("checked").ShouldBeTrue();
    }
}
