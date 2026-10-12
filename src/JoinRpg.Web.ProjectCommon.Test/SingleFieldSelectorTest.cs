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
        // Non-interactive: TypedSelector не пытается импортировать JS-модуль bootstrap-select.
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

        cut.FindAll("option").Select(o => o.TextContent).ShouldBe(["Строка персонажа"]);
        cut.Find("select").HasAttribute("multiple").ShouldBeFalse();
    }

    [Fact]
    public void WithoutSelection_EmptyOptionIsSelected()
    {
        using var ctx = CreateStaticContext();

        var cut = ctx.Render<SingleFieldSelector>(p => p
            .Add(x => x.ProjectId, ProjectId)
            .Add(x => x.AllowEmpty, true)
            .Add(x => x.EmptyText, EmptyText));

        var options = cut.FindAll("option");
        options[0].TextContent.ShouldBe(EmptyText);
        options[0].HasAttribute("selected").ShouldBeTrue();
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

        var options = cut.FindAll("option");
        options[0].HasAttribute("selected").ShouldBeFalse();
        options[1].HasAttribute("selected").ShouldBeTrue();
    }
}
